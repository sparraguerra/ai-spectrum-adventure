namespace AI.SpectrumAdventure.Application.Rules;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;

/// <summary>FR-013/FR-014: validates using a possessed item; delegates to PuzzleRules when the target is the puzzle entrance.</summary>
public static class ItemUsageRules
{
    public static RulesValidationResult ValidateUse(ItemId itemId, ItemId? targetId, Game game)
    {
        if (!game.Player.Inventory.Contains(itemId))
        {
            return RulesValidationResult.Fail(RulesFailureReason.ItemNotInInventory);
        }

        if (targetId is not null && targetId.Value.Value == game.Puzzle.Id.Value)
        {
            return PuzzleRules.ValidateSolve(itemId, game);
        }

        return RulesValidationResult.Ok(new ItemUsedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, itemId, targetId));
    }
}
