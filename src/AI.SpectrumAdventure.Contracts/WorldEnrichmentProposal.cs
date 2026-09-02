namespace AI.SpectrumAdventure.Contracts;

public sealed record WorldEnrichmentContext(
    Guid WorldId,
    string LocationId,
    string RegionId,
    int SceneVersion,
    string FactualName,
    string FactualDescription,
    IReadOnlyCollection<string> AllowedLoreKeys,
    IReadOnlyCollection<string> AllowedVisualCharacteristics);

public sealed record WorldEnrichmentProposal(
    string LocationId,
    int SceneVersion,
    string? DisplayName,
    string? Description,
    string? Atmosphere,
    IReadOnlyCollection<string> VisualCharacteristics,
    IReadOnlyCollection<string> LoreClaimKeys);

public sealed record WorldEnrichmentValidationResult(bool IsValid, string? FailureReason)
{
    public static WorldEnrichmentValidationResult Valid() => new(true, null);

    public static WorldEnrichmentValidationResult Invalid(string reason) => new(false, reason);
}