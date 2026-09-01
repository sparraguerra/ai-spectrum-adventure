namespace AI.SpectrumAdventure.Application.Rules;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;

/// <summary>FR-018/FR-019/FR-020: gates the Forgotten Tower entrance behind the dual item+clue condition, via Puzzle.CanSolve.</summary>
public static class PuzzleRules
{
    public static RulesValidationResult ValidateSolve(ItemId itemBeingUsed, Game game)
    {
        var puzzle = game.Puzzle;
        if (itemBeingUsed.Value != puzzle.RequiredItemId.Value)
        {
            return RulesValidationResult.Fail(RulesFailureReason.PuzzleConditionNotMet);
        }

        var hasItem = game.Player.Inventory.Contains(puzzle.RequiredItemId);
        var knowsClue = game.Player.KnownClues.Contains(puzzle.RequiredClueKey);

        if (!puzzle.CanSolve(hasItem, knowsClue))
        {
            return RulesValidationResult.Fail(RulesFailureReason.PuzzleConditionNotMet);
        }

        var now = DateTimeOffset.UtcNow;
        return RulesValidationResult.Ok(
            new ItemUsedEvent(Guid.NewGuid(), now, itemBeingUsed, null),
            new PuzzleSolvedEvent(Guid.NewGuid(), now, puzzle.Id));
    }
}
