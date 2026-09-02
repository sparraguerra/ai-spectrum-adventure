namespace AI.SpectrumAdventure.Application.Authoring;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Authoring;

public enum VersionHistoryOperationStatus
{
    Succeeded,
    NotFound,
    DuplicateIdentifier,
    Invalid
}

public sealed record VersionHistoryUseCaseResult(
    VersionHistoryOperationStatus Status,
    IReadOnlyList<AdventureVersion> Versions,
    AdventureDraft? RestoredDraft = null,
    IReadOnlyList<string>? Issues = null)
{
    public bool Succeeded => Status == VersionHistoryOperationStatus.Succeeded;
    public IReadOnlyList<string> Errors => Issues ?? [];
}

public sealed class VersionHistoryUseCases(IAdventureAuthoringRepository repository)
{
    public async Task<IReadOnlyList<AdventureVersion>> GetHistoryAsync(
        AdventureDraftId draftId,
        CancellationToken cancellationToken = default) =>
        await GetHistoryWithTelemetryAsync(draftId, cancellationToken);

    private async Task<IReadOnlyList<AdventureVersion>> GetHistoryWithTelemetryAsync(AdventureDraftId draftId, CancellationToken cancellationToken)
    {
        using var telemetry = AuthoringTelemetry.Start("restore.history");
        return await repository.GetVersionsAsync(draftId, cancellationToken);
    }

    public async Task<VersionHistoryUseCaseResult> RestoreAsNewDraftAsync(
        AdventureDraftId draftId,
        AdventureVersionId versionId,
        string newAdventureIdentifier,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        using var telemetry = AuthoringTelemetry.Start("restore");
        if (string.IsNullOrWhiteSpace(newAdventureIdentifier))
            return Failure(VersionHistoryOperationStatus.Invalid, "A new adventure identifier is required.");

        var sourceDraft = await repository.FindDraftAsync(draftId, cancellationToken);
        if (sourceDraft is null)
            return Failure(VersionHistoryOperationStatus.NotFound, "The source adventure draft was not found.");

        if (await repository.FindDraftByIdentifierAsync(newAdventureIdentifier, cancellationToken) is not null)
            return Failure(VersionHistoryOperationStatus.DuplicateIdentifier, "An adventure with the new identifier already exists.");

        var versions = await repository.GetVersionsAsync(draftId, cancellationToken);
        var version = versions.SingleOrDefault(candidate => candidate.Id == versionId);
        if (version is null)
            return Failure(VersionHistoryOperationStatus.NotFound, "The published adventure version was not found.");

        var restored = new AdventureDraft(
            AdventureDraftId.New(),
            newAdventureIdentifier,
            string.IsNullOrWhiteSpace(title) ? $"{sourceDraft.Title} (restored v{version.Sequence})" : title,
            sourceDraft.StartingLocationId,
            version.DefinitionJson);

        await repository.SaveDraftAsync(restored, -1, cancellationToken);
        await repository.AddAuditEntryAsync(new AuthoringAuditEntry(
            Guid.NewGuid(), restored.Id, AuthoringAction.Restored, DateTimeOffset.UtcNow, $"version:{version.Id.Value}"), cancellationToken);

        return new(VersionHistoryOperationStatus.Succeeded, versions, restored);
    }

    private static VersionHistoryUseCaseResult Failure(VersionHistoryOperationStatus status, string issue) =>
        new(status, [], Issues: [issue]);
}