namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Narrator;
using AI.SpectrumAdventure.Agents.Tests.TestDoubles;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;
using Xunit;

/// <summary>The Narrator's fallback narration must stay consistent with the RulesValidationResult reason that
/// produced it, even when the agent itself fails.</summary>
public class NarratorFailureNarrationTests
{
    [Theory]
    [InlineData("There is no path in that direction.")]
    [InlineData("You don't have that.")]
    public async Task NarrateAsync_AgentFailsTwice_FallsBackToTheProvidedFailureReasonText(string failureReason)
    {
        var runner = new FakeAgentRunner()
            .EnqueueFailure(new InvalidOperationException("schema invalid"))
            .EnqueueFailure(new InvalidOperationException("schema invalid"));
        var agent = new NarratorAgent(runner);
        var context = new NarratorContext("Forest Entrance", "base", [], [], ["North"], false, failureReason, failureReason);

        var result = await agent.NarrateAsync(context);

        result.Narration.Should().Be(failureReason);
        result.SceneChanged.Should().BeFalse();
    }

    [Fact]
    public async Task NarrateAsync_SuccessfulBlockedMovementContext_FallbackUsesActionSummary()
    {
        var runner = new FakeAgentRunner()
            .EnqueueFailure(new InvalidOperationException("schema invalid"))
            .EnqueueFailure(new InvalidOperationException("schema invalid"));
        var agent = new NarratorAgent(runner);
        var context = new NarratorContext("Dark Forest", "base", [], [], ["South"], true, null, "You take the Rusted Bridge Key.");

        var result = await agent.NarrateAsync(context);

        result.Narration.Should().Be("You take the Rusted Bridge Key.");
    }
}
