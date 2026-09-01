namespace AI.SpectrumAdventure.Agents.Narrator;

using System.Text;
using System.Text.Json;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;

/// <summary>Turns a validated action outcome into narrative text (constitution's Game Director / Narrator).
/// Falls back to a minimal deterministic description on any agent/schema failure, so gameplay is never blocked.</summary>
public sealed class NarratorAgent(IAgentRunner agentRunner) : INarratorAgent
{
    public Task<NarrationResult> NarrateAsync(NarratorContext context, CancellationToken cancellationToken = default) =>
        AgentTelemetry.TrackAsync(nameof(NarratorAgent), () =>
            StructuredOutputValidator.ExecuteAsync(
                ct => agentRunner.RunAsync<NarrationResult>(BuildPrompt(context), ct),
                fallback: () => BuildFallback(context),
                cancellationToken: cancellationToken));

    private static string BuildPrompt(NarratorContext context)
    {
        var sb = new StringBuilder(NarratorInstructions.SystemPrompt);
        sb.AppendLine().AppendLine("Context:");
        sb.AppendLine(JsonSerializer.Serialize(context));
        return sb.ToString();
    }

    /// <summary>Minimal deterministic description used when the Narrator agent fails or returns invalid output.</summary>
    private static NarrationResult BuildFallback(NarratorContext context)
    {
        var narration = context.ActionSucceeded
            ? context.ActionSummary
            : context.FailureReason ?? "Nothing happens.";

        return new NarrationResult(narration, Mood: null, context.ActionSucceeded, ImportantEvents: []);
    }
}
