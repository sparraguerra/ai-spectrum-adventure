namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Contracts;

/// <summary>Produces optional presentation data from a committed, read-only world projection.</summary>
public interface IWorldEnrichmentAgent
{
    Task<WorldEnrichmentProposal> EnrichAsync(WorldEnrichmentContext context, CancellationToken cancellationToken = default);
}

public interface IWorldEnrichmentRepository
{
    Task<WorldPresentationMetadata?> FindAsync(Guid worldId, string locationId, int sceneVersion, CancellationToken cancellationToken = default);

    Task SaveAsync(WorldPresentationMetadata metadata, CancellationToken cancellationToken = default);
}

public sealed record WorldPresentationMetadata(
    Guid WorldId,
    string LocationId,
    int SceneVersion,
    string FactualName,
    string FactualDescription,
    string? DisplayName,
    string? Description,
    string? Atmosphere,
    IReadOnlyCollection<string> VisualCharacteristics);