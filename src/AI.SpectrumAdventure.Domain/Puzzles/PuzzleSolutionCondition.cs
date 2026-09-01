namespace AI.SpectrumAdventure.Domain.Puzzles;

using AI.SpectrumAdventure.Domain.Common;

/// <summary>Encapsulates the Forgotten Tower's dual-condition solution rule (Clarification Q1, FR-019).</summary>
public sealed record PuzzleSolutionCondition(ItemId RequiredItemId, string RequiredClueKey)
{
    public bool IsSatisfiedBy(bool hasRequiredItem, bool knowsRequiredClue) => hasRequiredItem && knowsRequiredClue;
}
