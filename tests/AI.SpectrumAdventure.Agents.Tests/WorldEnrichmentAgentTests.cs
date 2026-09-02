namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Tests.TestDoubles;
using AI.SpectrumAdventure.Agents.WorldEnrichment;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;

public sealed class WorldEnrichmentAgentTests
{
    [Fact]
    public async Task EnrichAsync_PreservesTheScopedLocationAndSceneVersion()
    {
        var runner = new FakeAgentRunner().EnqueueResult(new WorldEnrichmentProposal("wrong", 99, "Mist Path", "Mist rises.", "eerie", ["mist"], []));
        var context = new WorldEnrichmentContext(Guid.NewGuid(), "forest-path", "wilderness", 1, "Forest Path", "A path.", [], ["mist"]);

        var proposal = await new WorldEnrichmentAgent(runner).EnrichAsync(context);

        proposal.LocationId.Should().Be(context.LocationId);
        proposal.SceneVersion.Should().Be(context.SceneVersion);
    }
}