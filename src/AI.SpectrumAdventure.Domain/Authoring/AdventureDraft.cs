namespace AI.SpectrumAdventure.Domain.Authoring;

public sealed class AdventureDraft
{
    public AdventureDraftId Id { get; }
    public string AdventureIdentifier { get; }
    public string Title { get; private set; }
    public string StartingLocationId { get; private set; }
    public string DefinitionJson { get; private set; }
    public long Revision { get; private set; }
    public AdventureDraftStatus Status { get; private set; }
    public AuthoringValidationResult? LastValidation { get; private set; }
    public AdventureVersionId? CurrentVersionId { get; private set; }

    public AdventureDraft(AdventureDraftId id, string adventureIdentifier, string title, string startingLocationId, string definitionJson, long revision = 0, AdventureDraftStatus status = AdventureDraftStatus.Draft)
    {
        if (string.IsNullOrWhiteSpace(adventureIdentifier)) throw new ArgumentException("An adventure identifier is required.", nameof(adventureIdentifier));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("A title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(startingLocationId)) throw new ArgumentException("A starting location is required.", nameof(startingLocationId));
        if (string.IsNullOrWhiteSpace(definitionJson)) throw new ArgumentException("A definition is required.", nameof(definitionJson));
        Id = id;
        AdventureIdentifier = adventureIdentifier;
        Title = title;
        StartingLocationId = startingLocationId;
        DefinitionJson = definitionJson;
        Revision = revision;
        Status = status;
    }

    public void Update(string title, string startingLocationId, string definitionJson, AuthoringValidationResult validation)
    {
        if (validation.HasBlockingIssues) throw new InvalidOperationException("A draft cannot accept a definition with blocking validation issues.");
        Title = title;
        StartingLocationId = startingLocationId;
        DefinitionJson = definitionJson;
        LastValidation = validation;
        Revision++;
    }

    public void RecordValidation(AuthoringValidationResult validation) => LastValidation = validation;

    public void RestoreMetadata(AuthoringValidationResult? validation, AdventureVersionId? currentVersionId)
    {
        LastValidation = validation;
        CurrentVersionId = currentVersionId;
    }

    public void MarkPublished(AdventureVersionId versionId)
    {
        CurrentVersionId = versionId;
        Status = AdventureDraftStatus.Published;
    }
}