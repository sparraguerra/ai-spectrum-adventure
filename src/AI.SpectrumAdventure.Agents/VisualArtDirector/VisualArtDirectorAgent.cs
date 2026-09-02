namespace AI.SpectrumAdventure.Agents.VisualArtDirector;

using System.Text.Json;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;

/// <summary>Transforms validated scene state into a retro-styled visual specification (constitution's Visual Art
/// Director). Never mutates GameState and never becomes the authoritative scene representation (Principle XXI).</summary>
public sealed class VisualArtDirectorAgent(IAgentRunner agentRunner) : IVisualArtDirector
{
    public Task<VisualSceneSpec> DescribeSceneAsync(VisualContext context, CancellationToken cancellationToken = default) =>
        AgentTelemetry.TrackAsync(nameof(VisualArtDirectorAgent), () =>
            StructuredOutputValidator.ExecuteAsync(
                async ct =>
                {
                    var spec = await agentRunner.RunAsync<VisualSceneSpec>(BuildPrompt(context), ct);
                    // Never trust the LLM's echoed key — it must match the deterministic cache key exactly,
                    // otherwise the blob name and the VisualAsset DB record end up keyed differently.
                    return spec with { SceneStateKey = context.SceneStateKey, VisualCharacteristics = context.VisualCharacteristics };
                },
                fallback: () => BuildFallback(context),
                cancellationToken: cancellationToken));

    private static string BuildPrompt(VisualContext context) =>
        RetroStyleConstraints.SystemPrompt + Environment.NewLine + "Scene: " + JsonSerializer.Serialize(context);

    /// <summary>Deterministic fallback used when the agent fails or returns invalid output — still retro-styled
    /// and still derived only from the provided context (never invented).</summary>
    private static VisualSceneSpec BuildFallback(VisualContext context) =>
        new(
            context.LocationId,
            context.SceneStateKey,
            TimeOfDay: null,
            Objects: [.. context.VisibleObjectNames.Take(12)],
            context.Mood,
            RetroStyleConstraints.StyleTag,
            context.VisualCharacteristics);
}
