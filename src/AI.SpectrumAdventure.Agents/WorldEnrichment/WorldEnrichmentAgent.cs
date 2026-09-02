namespace AI.SpectrumAdventure.Agents.WorldEnrichment;

using System.Text.Json;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;

public sealed class WorldEnrichmentAgent(IAgentRunner agentRunner) : IWorldEnrichmentAgent
{
    public Task<WorldEnrichmentProposal> EnrichAsync(WorldEnrichmentContext context, CancellationToken cancellationToken = default) =>
        AgentTelemetry.TrackAsync(nameof(WorldEnrichmentAgent), () =>
            StructuredOutputValidator.ExecuteAsync(
                async cancellationToken =>
                {
                    var proposal = await agentRunner.RunAsync<WorldEnrichmentProposal>(BuildPrompt(context), cancellationToken);
                    return proposal with { LocationId = context.LocationId, SceneVersion = context.SceneVersion };
                },
                fallback: () => new(context.LocationId, context.SceneVersion, null, null, null, [], []),
                cancellationToken: cancellationToken));

    private static string BuildPrompt(WorldEnrichmentContext context) =>
        WorldEnrichmentInstructions.SystemPrompt + Environment.NewLine + JsonSerializer.Serialize(context);
}