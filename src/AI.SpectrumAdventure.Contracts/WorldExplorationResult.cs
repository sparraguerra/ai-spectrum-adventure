namespace AI.SpectrumAdventure.Contracts;

/// <summary>Player-facing result of attempting to travel through a world boundary.</summary>
public sealed record WorldExplorationResult(
    bool Success,
    string WorldId,
    string? LocationId,
    bool NewlyDiscovered,
    bool MapUpdated,
    string? FailureReason);