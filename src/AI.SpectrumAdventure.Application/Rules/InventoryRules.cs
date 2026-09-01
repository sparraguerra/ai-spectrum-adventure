namespace AI.SpectrumAdventure.Application.Rules;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Items;

/// <summary>FR-011/FR-014: validates taking a collectible item into inventory.</summary>
public static class InventoryRules
{
    public static RulesValidationResult ValidateTake(ItemId itemId, Game game)
    {
        if (game.Player.Inventory.Contains(itemId))
        {
            return RulesValidationResult.Fail(RulesFailureReason.AlreadyInState);
        }

        if (!game.CurrentLocation.ObjectIds.Contains(itemId))
        {
            return RulesValidationResult.Fail(RulesFailureReason.TargetNotPresent);
        }

        var item = game.GetItem(itemId);
        if (!item.HasState(ItemState.Collectible))
        {
            return RulesValidationResult.Fail(RulesFailureReason.ImpossibleAction);
        }

        return RulesValidationResult.Ok(new ItemTakenEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, itemId, game.Player.CurrentLocationId));
    }
}
