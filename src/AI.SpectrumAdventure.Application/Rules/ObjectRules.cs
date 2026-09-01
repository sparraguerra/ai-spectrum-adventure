namespace AI.SpectrumAdventure.Application.Rules;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;

/// <summary>FR-002/FR-006/Edge Case "Hidden objects": validates that an object is present and revealed before it can be examined.</summary>
public static class ObjectRules
{
    public static RulesValidationResult Validate(ItemId itemId, Game game)
    {
        var isPresentHere = game.CurrentLocation.ObjectIds.Contains(itemId) || game.Player.Inventory.Contains(itemId);
        if (!isPresentHere)
        {
            return RulesValidationResult.Fail(RulesFailureReason.TargetNotPresent);
        }

        var item = game.GetItem(itemId);
        if (!item.IsRevealed(game.WorldFlagKeys))
        {
            return RulesValidationResult.Fail(RulesFailureReason.TargetNotPresent);
        }

        // Examine never mutates state — it produces no proposed events, only a (later) Narrator description.
        return RulesValidationResult.Ok();
    }
}
