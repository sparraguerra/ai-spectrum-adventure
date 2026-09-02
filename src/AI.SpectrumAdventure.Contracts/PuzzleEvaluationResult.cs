namespace AI.SpectrumAdventure.Contracts;

/// <summary>The deterministic, player-facing assessment of one attempted puzzle solution.</summary>
public sealed record PuzzleEvaluationResult(
    string PuzzleId,
    bool Accepted,
    string PreviousState,
    string CurrentState,
    IReadOnlyCollection<string> ConsequenceIds,
    string? RejectionReason);