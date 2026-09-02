namespace AI.SpectrumAdventure.Domain.Puzzles;

using AI.SpectrumAdventure.Domain.Common;

public enum PuzzleState
{
    Unknown,
    Discovered,
    Investigating,
    Blocked,
    Solved,
}

public enum PuzzleConditionType
{
    ItemPossessed,
    ClueKnown,
    WorldFlagSet,
    PuzzleSolved,
}

public enum PuzzleOutcomeType
{
    Clue,
    Item,
    NpcInteraction,
    Location,
    Connection,
    WorldEvent,
    FollowOnPuzzle,
}

public sealed record PuzzlePrerequisiteReference(PuzzleConditionType Type, string ReferenceId);

public sealed record PuzzleCondition(PuzzleConditionType Type, string ReferenceId);

public sealed record PuzzleSolutionDefinition(string Id, IReadOnlyCollection<PuzzleCondition> Conditions)
{
    public bool IsSatisfiedBy(Func<PuzzleCondition, bool> isConditionSatisfied) =>
        Conditions.Count > 0 && Conditions.All(isConditionSatisfied);
}

public sealed record PuzzleOutcomeDefinition(string Id, PuzzleOutcomeType Type, string ReferenceId);

public sealed record PuzzleChainLink(PuzzleId NextPuzzleId);