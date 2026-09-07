namespace AI.SpectrumAdventure.Application.Authoring;

using System.Text.Json;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Authoring;

public enum DraftOperationStatus
{
    Succeeded,
    NotFound,
    DuplicateIdentifier,
    Conflict,
    Invalid
}

public sealed record DraftUseCaseResult(
    DraftOperationStatus Status,
    AdventureDraft? Draft = null,
    IReadOnlyList<string>? Issues = null)
{
    public bool Succeeded => Status == DraftOperationStatus.Succeeded;
    public IReadOnlyList<string> Errors => Issues ?? [];
}

public sealed class DraftUseCases(
    IAdventureAuthoringRepository repository,
    IAdventureValidator validator)
{
    public async Task<DraftUseCaseResult> CreateAsync(AdventureDraftRequest request, CancellationToken cancellationToken = default)
    {
        using var telemetry = AuthoringTelemetry.Start("draft.create");
        var existing = await repository.FindDraftByIdentifierAsync(request.Id, cancellationToken);
        if (existing is not null)
        {
            return Conflict(DraftOperationStatus.DuplicateIdentifier, "An adventure with this identifier already exists.");
        }

        AdventureDraft draft;
        try
        {
            draft = new AdventureDraft(
                AdventureDraftId.New(),
                request.Id,
                request.Title,
                request.StartingLocationId,
                SerializeDefinition(request.Definition));
        }
        catch (ArgumentException exception)
        {
            return Conflict(DraftOperationStatus.Invalid, exception.Message);
        }

        var validation = validator.Validate(draft);
        if (validation.HasBlockingIssues)
        {
            return new DraftUseCaseResult(DraftOperationStatus.Invalid, draft, validation.Issues.Select(issue => issue.Reason).ToArray());
        }

        await repository.SaveDraftAsync(draft, -1, cancellationToken);
        return new DraftUseCaseResult(DraftOperationStatus.Succeeded, draft);
    }

    public async Task<DraftUseCaseResult> LoadAsync(string adventureIdentifier, CancellationToken cancellationToken = default)
    {
        using var telemetry = AuthoringTelemetry.Start("draft.load");
        var draft = await repository.FindDraftByIdentifierAsync(adventureIdentifier, cancellationToken);
        return draft is null
            ? Conflict(DraftOperationStatus.NotFound, "The adventure draft was not found.")
            : new DraftUseCaseResult(DraftOperationStatus.Succeeded, draft);
    }

    public async Task<DraftUseCaseResult> SaveAsync(
        AdventureDraftId draftId,
        long expectedRevision,
        AdventureDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        using var telemetry = AuthoringTelemetry.Start("draft.save");
        var draft = await repository.FindDraftAsync(draftId, cancellationToken);
        if (draft is null)
        {
            return Conflict(DraftOperationStatus.NotFound, "The adventure draft was not found.");
        }

        if (draft.Revision != expectedRevision)
        {
            return Conflict(DraftOperationStatus.Conflict, "This draft changed elsewhere. Reload it before saving.");
        }

        AdventureDraft candidate;
        try
        {
            candidate = new AdventureDraft(draft.Id, draft.AdventureIdentifier, request.Title, request.StartingLocationId, SerializeDefinition(request.Definition), draft.Revision);
        }
        catch (ArgumentException exception)
        {
            return new DraftUseCaseResult(DraftOperationStatus.Invalid, draft, [exception.Message]);
        }

        var validation = validator.Validate(candidate);
        if (validation.HasBlockingIssues)
        {
            return new DraftUseCaseResult(DraftOperationStatus.Invalid, draft, validation.Issues.Select(issue => issue.Reason).ToArray());
        }

        candidate.Update(request.Title, request.StartingLocationId, candidate.DefinitionJson, validation);
        try
        {
            await repository.SaveDraftAsync(candidate, expectedRevision, cancellationToken);
        }
        catch (Exception exception) when (IsConcurrencyConflict(exception))
        {
            return Conflict(DraftOperationStatus.Conflict, "This draft changed elsewhere. Reload it before saving.");
        }

        return new DraftUseCaseResult(DraftOperationStatus.Succeeded, candidate);
    }

    private static string SerializeDefinition(object definition) =>
        definition is string json ? json : JsonSerializer.Serialize(definition);

    private static DraftUseCaseResult Conflict(DraftOperationStatus status, string issue) =>
        new(status, Issues: [issue]);

    private static bool IsConcurrencyConflict(Exception exception) =>
        exception.GetType().FullName == "Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException" ||
        exception is DraftRevisionConflictException;
}

public sealed class DraftRevisionConflictException(string message) : Exception(message);