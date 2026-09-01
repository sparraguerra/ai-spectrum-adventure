namespace AI.SpectrumAdventure.Application.Rules;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;

/// <summary>Validates that an NPC the player wants to talk to is actually present at the current location.</summary>
public static class NpcPresenceRules
{
    public static RulesValidationResult ValidateTalkTo(NpcId npcId, Game game)
    {
        if (!game.CurrentLocation.NpcIds.Contains(npcId))
        {
            return RulesValidationResult.Fail(RulesFailureReason.TargetNotPresent);
        }

        // No GameEvent is produced here — dialogue effects (e.g., learning a clue) are proposed by the NPC Agent
        // (Phase 11) and applied only after the orchestrator validates them against the NPC's KnowledgeBoundary.
        return RulesValidationResult.Ok();
    }
}
