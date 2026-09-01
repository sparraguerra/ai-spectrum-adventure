namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Narrator;
using AI.SpectrumAdventure.Agents.Tests.TestDoubles;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;
using Xunit;

public class NarratorAgentTests
{
    private static NarratorContext SuccessfulMoveContext() => new(
        "Dark Forest",
        "Towering, ancient trees block out most of the light here.",
        VisibleObjectNames: [],
        PresentCharacterNames: ["The Hermit"],
        AvailableExits: ["South", "East"],
        ActionSucceeded: true,
        FailureReason: null,
        ActionSummary: "Towering, ancient trees block out most of the light here.");

    [Fact]
    public async Task NarrateAsync_SuccessfulAction_ReturnsAgentNarration()
    {
        var runner = new FakeAgentRunner().EnqueueResult(new NarrationResult(
            "The trees loom overhead as you step into the Dark Forest.", "mysterious", true, []));
        var agent = new NarratorAgent(runner);

        var result = await agent.NarrateAsync(SuccessfulMoveContext());

        result.Narration.Should().Contain("Dark Forest".Split(' ')[0]);
        result.SceneChanged.Should().BeTrue();
    }

    [Fact]
    public async Task NarrateAsync_ExamineAction_NarratesTheProvidedObjectDescription()
    {
        var context = new NarratorContext(
            "Forest Entrance", "base", [], [], ["North"], true, null, "A weathered wooden sign, half-swallowed by moss.");
        var runner = new FakeAgentRunner().EnqueueResult(new NarrationResult(
            "You lean closer and read the weathered sign, half-swallowed by moss.", null, false, []));
        var agent = new NarratorAgent(runner);

        var result = await agent.NarrateAsync(context);

        result.Narration.Should().NotBeNullOrWhiteSpace();
    }
}
