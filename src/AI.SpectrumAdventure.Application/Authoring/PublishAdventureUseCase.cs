namespace AI.SpectrumAdventure.Application.Authoring;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Authoring;

public sealed record PublishAdventureUseCaseResult(bool Published, AdventureVersion? Version, AuthoringValidationResult Validation, IReadOnlyList<string> Issues)
{
    public Guid? VersionId => Version?.Id.Value;
    public long? VersionSequence => Version?.Sequence;
}

public sealed class PublishAdventureUseCase(IAdventureAuthoringRepository repository, IAdventureValidator validator)
{
    public async Task<PublishAdventureUseCaseResult> ExecuteAsync(AdventureDraftId draftId, long expectedRevision, CancellationToken cancellationToken = default)
    {
        using var telemetry = AuthoringTelemetry.Start("publication");
        var draft = await repository.FindDraftAsync(draftId, cancellationToken);
        if (draft is null)
            return Failure(draftId, expectedRevision, "The adventure draft was not found.");
        if (draft.Revision != expectedRevision)
            return Failure(draft.Id, draft.Revision, "The draft changed elsewhere. Reload it before publishing.");

        var validation = validator.Validate(draft);
        draft.RecordValidation(validation);
        if (validation.HasBlockingIssues)
            return new(false, null, validation, validation.Issues.Select(issue => $"{issue.Element}: {issue.Reason}").ToArray());

        var versions = await repository.GetVersionsAsync(draft.Id, cancellationToken);
        var version = new AdventureVersion(AdventureVersionId.New(), draft.Id, draft.AdventureIdentifier,
            versions.Count == 0 ? 1 : versions.Max(existing => existing.Sequence) + 1,
            draft.DefinitionJson, DateTimeOffset.UtcNow);
        draft.MarkPublished(version.Id);
        try
        {
            await repository.PublishAsync(draft, version, expectedRevision, cancellationToken);
        }
        catch (Exception exception) when (exception.GetType().FullName == "Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException" || exception is DraftRevisionConflictException)
        {
            return Failure(draft.Id, draft.Revision, "The draft changed elsewhere. Reload it before publishing.");
        }
        return new(true, version, validation, []);
    }

    private static PublishAdventureUseCaseResult Failure(AdventureDraftId draftId, long revision, string reason)
    {
        var validation = new AuthoringValidationResult(draftId, revision, [new AuthoringValidationIssue("draft", reason, AuthoringIssueSeverity.Blocking)]);
        return new(false, null, validation, [reason]);
    }
}