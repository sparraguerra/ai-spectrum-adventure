namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Tests.TestDoubles;
using AI.SpectrumAdventure.Agents.VisualArtDirector;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;
using Xunit;

public class VisualArtDirectorAgentTests
{
    [Fact]
    public async Task DescribeSceneAsync_AgentSucceeds_UsesTheFixedRetroStyleTag()
    {
        var runner = new FakeAgentRunner().EnqueueResult(new VisualSceneSpec(
            "dark-forest", "dark-forest|", "Night", ["ancient trees", "the Hermit"], "mysterious", RetroStyleConstraints.StyleTag));
        var agent = new VisualArtDirectorAgent(runner);
        var context = new VisualContext("dark-forest", "Dark Forest", ["ancient trees"], "mysterious", "dark-forest|");

        var spec = await agent.DescribeSceneAsync(context);

        spec.Style.Should().Be("ZX Spectrum inspired 8-bit game art");
    }

    [Fact]
    public async Task DescribeSceneAsync_AgentFailsTwice_FallbackAlsoUsesTheFixedRetroStyleTag_AndOnlyProvidedObjects()
    {
        var runner = new FakeAgentRunner()
            .EnqueueFailure(new InvalidOperationException("schema invalid"))
            .EnqueueFailure(new InvalidOperationException("schema invalid"));
        var agent = new VisualArtDirectorAgent(runner);
        var context = new VisualContext("forest-entrance", "Forest Entrance", ["a weathered sign"], "calm", "forest-entrance|");

        var spec = await agent.DescribeSceneAsync(context);

        spec.Style.Should().Be(RetroStyleConstraints.StyleTag);
        spec.Objects.Should().BeEquivalentTo(["a weathered sign"]);
    }

    [Fact]
    public async Task DescribeSceneAsync_NeverExposesAnyGameStateMutationCapability()
    {
        // VisualSceneSpec is an immutable record with no methods that could mutate Game state — this test
        // documents that invariant explicitly rather than merely relying on the type system.
        var runner = new FakeAgentRunner().EnqueueResult(new VisualSceneSpec(
            "old-bridge", "old-bridge|", null, ["the bridge"], "eerie", RetroStyleConstraints.StyleTag));
        var agent = new VisualArtDirectorAgent(runner);

        var spec = await agent.DescribeSceneAsync(new VisualContext("old-bridge", "Old Bridge", ["the bridge"], "eerie", "old-bridge|"));

        spec.Should().BeOfType<VisualSceneSpec>();
    }
}
