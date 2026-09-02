namespace AI.SpectrumAdventure.Contracts;

/// <summary>Structured request identifying one deterministic expansion boundary.</summary>
public sealed record WorldGenerationRequest(
    string WorldId,
    string GenerationKey,
    string SourceLocationId,
    string Direction,
    string GenerationVersion);