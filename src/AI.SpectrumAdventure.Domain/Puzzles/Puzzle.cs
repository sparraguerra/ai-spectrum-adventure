namespace AI.SpectrumAdventure.Domain.Puzzles;

using AI.SpectrumAdventure.Domain.Common;

/// <summary>The Forgotten Tower entrance obstacle (spec Key Entity "Puzzle"); FR-018/FR-020/FR-021.</summary>
public sealed class Puzzle
{
    public PuzzleId Id { get; }
    public PuzzleSolutionCondition SolutionCondition { get; }
    public bool Solved { get; private set; }

    public ItemId RequiredItemId => SolutionCondition.RequiredItemId;
    public string RequiredClueKey => SolutionCondition.RequiredClueKey;

    public Puzzle(PuzzleId id, PuzzleSolutionCondition solutionCondition)
    {
        Id = id;
        SolutionCondition = solutionCondition;
    }

    /// <summary>Pure, independently-testable gate (FR-019/FR-020): never solvable twice, never solvable by any other means.</summary>
    public bool CanSolve(bool hasRequiredItem, bool knowsRequiredClue) =>
        !Solved && SolutionCondition.IsSatisfiedBy(hasRequiredItem, knowsRequiredClue);

    /// <summary>Idempotency guard (T055): applying PuzzleSolved a second time has no additional effect.</summary>
    internal void MarkSolved() => Solved = true;
}
