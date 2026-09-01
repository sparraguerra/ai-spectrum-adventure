namespace AI.SpectrumAdventure.Agents.Npc;

using System.Text.Json;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;

/// <summary>
/// Generates in-character NPC dialogue strictly bounded to the NPC's authoritative KnowledgeBoundary (FR-016/FR-017).
/// Any reply proposing a knowledge key outside that boundary is rejected client-side (see FilterToKnowledgeBoundary)
/// before the orchestrator ever sees it, so the NPC cannot leak information no matter how it phrases a reply.
/// </summary>
public sealed class NpcAgent(IAgentRunner agentRunner) : INpcAgent
{
    public async Task<NpcResponse> ReplyAsync(NpcContext context, CancellationToken cancellationToken = default)
    {
        var response = await AgentTelemetry.TrackAsync(nameof(NpcAgent), () =>
            StructuredOutputValidator.ExecuteAsync(
                ct => agentRunner.RunAsync<NpcResponse>(BuildPrompt(context), ct),
                fallback: () => BuildFallback(context),
                cancellationToken: cancellationToken));

        return FilterToKnowledgeBoundary(response, context);
    }

    private static string BuildPrompt(NpcContext context)
    {
        var systemPrompt = string.Format(
            NpcInstructions.SystemPromptTemplate,
            context.NpcName,
            context.PersonalityProfile,
            string.Join(", ", context.KnowledgeBoundary));

        return systemPrompt + Environment.NewLine + "Recent conversation: " + JsonSerializer.Serialize(context.RecentConversation) +
            Environment.NewLine + "Player says: " + context.PlayerUtterance;
    }

    /// <summary>Enforces FR-017 defense-in-depth: strip any revealed key the agent proposed that isn't actually
    /// on the NPC's authoritative whitelist, regardless of what the (possibly hallucinating) model returned.</summary>
    private static NpcResponse FilterToKnowledgeBoundary(NpcResponse response, NpcContext context)
    {
        var allowedKeys = response.RevealedKnowledgeKeys.Where(context.KnowledgeBoundary.Contains).ToList();
        return response with { RevealedKnowledgeKeys = allowedKeys };
    }

    private static NpcResponse BuildFallback(NpcContext context) =>
        new(context.NpcName, "...", RevealedKnowledgeKeys: [], SuggestedTopics: null);
}
