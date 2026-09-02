namespace AI.SpectrumAdventure.Application.Authoring;

using System.Text.Json;
using System.Text.Json.Nodes;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Authoring;

public enum ProposalOperationStatus { Succeeded, NotFound, Invalid, Conflict }

public sealed record ProposalUseCaseResult(ProposalOperationStatus Status, AuthoringProposal? Proposal = null, AdventureDraft? Draft = null, IReadOnlyList<string>? Issues = null)
{
    public bool Succeeded => Status == ProposalOperationStatus.Succeeded;
    public IReadOnlyList<string> Errors => Issues ?? [];
}

public sealed class ProposalUseCases(
    IAdventureAuthoringRepository repository,
    IAuthoringProposalAgent proposalAgent,
    IAdventureValidator validator)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ProposalUseCaseResult> RequestAsync(AdventureDraftId draftId, string request, CancellationToken cancellationToken = default)
    {
        using var telemetry = AuthoringTelemetry.Start("proposal.request");
        var draft = await repository.FindDraftAsync(draftId, cancellationToken);
        if (draft is null) return Failure(ProposalOperationStatus.NotFound, "The adventure draft was not found.");
        var context = JsonSerializer.Serialize(new AuthoringProposalContext(draft.Id.Value, draft.AdventureIdentifier, draft.Title, draft.StartingLocationId, draft.DefinitionJson, request), JsonOptions);
        var contract = await proposalAgent.CreateProposalAsync(draft.Id.Value, request, context, cancellationToken);
        var output = AuthoringProposalContractValidator.Validate(contract);
        var patchJson = JsonSerializer.Serialize(contract.Patch, JsonOptions);
        var proposal = new AuthoringProposal(new AuthoringProposalId(contract.ProposalId == Guid.Empty ? Guid.NewGuid() : contract.ProposalId), draftId, contract.ContentType, request, patchJson);
        if (!output.IsValid) proposal.MarkInvalid(new AuthoringValidationResult(draftId, draft.Revision, output.Issues.Select(issue => new AuthoringValidationIssue("proposal", issue, AuthoringIssueSeverity.Blocking))));
        await repository.AddProposalAsync(proposal, cancellationToken);
        return new ProposalUseCaseResult(output.IsValid ? ProposalOperationStatus.Succeeded : ProposalOperationStatus.Invalid, proposal, draft, output.Issues);
    }

    public async Task<ProposalUseCaseResult> EditAsync(AuthoringProposal proposal, object patch, CancellationToken cancellationToken = default)
    {
        using var telemetry = AuthoringTelemetry.Start("proposal.edit");
        var edited = new AuthoringProposalContract(proposal.Id.Value, proposal.DraftId.Value, proposal.ContentType, patch, "pending");
        var output = AuthoringProposalContractValidator.Validate(edited);
        if (!output.IsValid) return Failure(ProposalOperationStatus.Invalid, output.Issues.ToArray(), proposal);
        proposal.Edit(JsonSerializer.Serialize(patch, JsonOptions));
        await repository.SaveProposalAsync(proposal, cancellationToken);
        return new ProposalUseCaseResult(ProposalOperationStatus.Succeeded, proposal, Issues: []);
    }

    public async Task<ProposalUseCaseResult> RejectAsync(AuthoringProposal proposal, CancellationToken cancellationToken = default)
    {
        using var telemetry = AuthoringTelemetry.Start("proposal.reject");
        proposal.Reject();
        await repository.SaveProposalAsync(proposal, cancellationToken);
        await repository.AddAuditEntryAsync(new AuthoringAuditEntry(Guid.NewGuid(), proposal.DraftId, AuthoringAction.ProposalRejected, DateTimeOffset.UtcNow, "author"), cancellationToken);
        return new ProposalUseCaseResult(ProposalOperationStatus.Succeeded, proposal);
    }

    public async Task<ProposalUseCaseResult> AcceptAsync(AuthoringProposal proposal, long expectedRevision, CancellationToken cancellationToken = default)
    {
        using var telemetry = AuthoringTelemetry.Start("proposal.accept");
        if (proposal.Status != ProposalStatus.Pending) return Failure(ProposalOperationStatus.Invalid, "Only a pending proposal can be accepted.", proposal);
        var draft = await repository.FindDraftAsync(proposal.DraftId, cancellationToken);
        if (draft is null) return Failure(ProposalOperationStatus.NotFound, "The adventure draft was not found.", proposal);
        if (draft.Revision != expectedRevision) return Failure(ProposalOperationStatus.Conflict, "The draft changed elsewhere. Reload it before accepting.", proposal, draft);

        string definition;
        string title = draft.Title;
        string start = draft.StartingLocationId;
        try { (definition, title, start) = ApplyPatch(draft, proposal.PatchJson); }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException)
        {
            return await MarkInvalidAsync(proposal, draft, exception.Message, cancellationToken);
        }

        var candidate = new AdventureDraft(draft.Id, draft.AdventureIdentifier, title, start, definition, draft.Revision);
        var validation = validator.Validate(candidate);
        if (validation.HasBlockingIssues) return await MarkInvalidAsync(proposal, draft, validation.Issues.Select(issue => issue.Reason).ToArray(), cancellationToken, validation);
        candidate.Update(title, start, definition, validation);
        await repository.SaveDraftAsync(candidate, expectedRevision, cancellationToken);
        proposal.Accept(validation);
        await repository.SaveProposalAsync(proposal, cancellationToken);
        await repository.AddAuditEntryAsync(new AuthoringAuditEntry(Guid.NewGuid(), proposal.DraftId, AuthoringAction.ProposalAccepted, DateTimeOffset.UtcNow, "author"), cancellationToken);
        return new ProposalUseCaseResult(ProposalOperationStatus.Succeeded, proposal, candidate);
    }

    private static (string Definition, string Title, string Start) ApplyPatch(AdventureDraft draft, string patchJson)
    {
        var patch = JsonSerializer.Deserialize<AuthoringProposalPatch>(patchJson, JsonOptions) ?? throw new JsonException("Proposal patch is invalid.");
        var definition = JsonNode.Parse(draft.DefinitionJson)?.AsObject() ?? throw new JsonException("Draft definition is invalid.");
        var title = draft.Title; var start = draft.StartingLocationId;
        foreach (var operation in patch.Operations)
        {
            if (operation.Path == "/title") { title = operation.Value?.GetString() ?? throw new JsonException("Title value is required."); continue; }
            if (operation.Path == "/startingLocationId") { start = operation.Value?.GetString() ?? throw new JsonException("Starting location value is required."); continue; }
            if (operation.Path != "/definition") throw new InvalidOperationException($"Patch path '{operation.Path}' is not supported.");
            if (operation.Operation == "remove") throw new InvalidOperationException("The complete definition cannot be removed.");
            definition = JsonNode.Parse(operation.Value?.GetString() ?? throw new JsonException("Definition value is required."))?.AsObject() ?? throw new JsonException("Definition value must be a JSON object.");
        }
        return (definition.ToJsonString(JsonOptions), title, start);
    }

    private async Task<ProposalUseCaseResult> MarkInvalidAsync(AuthoringProposal proposal, AdventureDraft draft, string issue, CancellationToken cancellationToken, AuthoringValidationResult? validation = null) =>
        await MarkInvalidAsync(proposal, draft, [issue], cancellationToken, validation);

    private async Task<ProposalUseCaseResult> MarkInvalidAsync(AuthoringProposal proposal, AdventureDraft draft, IReadOnlyList<string> issues, CancellationToken cancellationToken, AuthoringValidationResult? validation = null)
    {
        validation ??= new AuthoringValidationResult(draft.Id, draft.Revision, issues.Select(issue => new AuthoringValidationIssue("proposal", issue, AuthoringIssueSeverity.Blocking)));
        proposal.MarkInvalid(validation);
        await repository.SaveProposalAsync(proposal, cancellationToken);
        return new ProposalUseCaseResult(ProposalOperationStatus.Invalid, proposal, draft, issues);
    }

    private static ProposalUseCaseResult Failure(ProposalOperationStatus status, string issue, AuthoringProposal? proposal = null, AdventureDraft? draft = null) => new(status, proposal, draft, [issue]);
    private static ProposalUseCaseResult Failure(ProposalOperationStatus status, IReadOnlyList<string> issues, AuthoringProposal proposal) => new(status, proposal, Issues: issues);
}