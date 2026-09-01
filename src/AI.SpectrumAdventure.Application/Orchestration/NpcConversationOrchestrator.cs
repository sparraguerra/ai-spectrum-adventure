namespace AI.SpectrumAdventure.Application.Orchestration;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;

/// <summary>
/// Coordinates player-NPC conversation: builds context from the authoritative Npc, asks the NPC Agent for a
/// reply, then re-validates any "revealed knowledge" against the Npc's own KnowledgeBoundary before applying it
/// as an NpcRelationshipChangedEvent (defense-in-depth for FR-017, on top of NpcAgent's own filtering).
/// </summary>
public static class NpcConversationOrchestrator
{
    public static async Task<ActionResult> ConverseAsync(
        Game game,
        NpcId npcId,
        string playerUtterance,
        INpcAgent npcAgent,
        CancellationToken cancellationToken = default)
    {
        if (!game.CurrentLocation.NpcIds.Contains(npcId))
        {
            return BuildResult(game, success: false, "TargetNotPresent", "There's no one like that here to talk to.");
        }

        var npc = game.GetNpc(npcId);
        var context = new NpcContext(
            npc.Name,
            npc.PersonalityProfile,
            [.. npc.KnowledgeBoundary],
            [.. npc.ConversationMemory.TakeLast(3).Select(turn => $"{turn.PlayerUtterance} -> {turn.NpcReply}")],
            playerUtterance);

        var response = await npcAgent.ReplyAsync(context, cancellationToken);

        // Defense-in-depth (FR-017): only ever accept revealed keys that are on the *domain* Npc's whitelist.
        var validRevealedKeys = response.RevealedKnowledgeKeys.Where(npc.KnowledgeBoundary.Contains).ToList();

        var now = DateTimeOffset.UtcNow;
        var flagKey = validRevealedKeys.Contains(game.Puzzle.RequiredClueKey)
            ? WorldFlags.NpcGaveClue
            : $"talked-to-{npcId.Value}";

        game.Apply(new NpcRelationshipChangedEvent(Guid.NewGuid(), now, npcId, flagKey, playerUtterance, response.Reply));

        return BuildResult(game, success: true, reason: null, response.Reply);
    }

    private static ActionResult BuildResult(Game game, bool success, string? reason, string narrative) =>
        new(
            game.Id.Value,
            success,
            reason,
            narrative,
            SceneChanged: false,
            game.CurrentLocation.Id.Value,
            GetInventoryQuery.Execute(game),
            VisualAssetUrl: null);
}
