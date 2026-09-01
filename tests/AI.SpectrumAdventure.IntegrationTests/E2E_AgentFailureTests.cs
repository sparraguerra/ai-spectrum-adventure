namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Agents.Narrator;
using AI.SpectrumAdventure.Agents.Tests.TestDoubles;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;

public sealed class E2E_AgentFailureTests
{
    [Fact]
    public async Task NarratorFailure_PreservesValidGameStateAndUsesFallbackNarrative()
    {
        var game = E2ETestSupport.CreateStartedGame();
        var runner = new FakeAgentRunner()
            .EnqueueFailure(new InvalidOperationException())
            .EnqueueFailure(new InvalidOperationException());

        var result = await AdventureOrchestrator.ProcessActionWithNarrationAsync(
            game,
            E2ETestSupport.Intent(IntentAction.Go, "north"),
            new NarratorAgent(runner));

        result.Success.Should().BeTrue();
        result.Narrative.Should().Contain("ancient trees");
        game.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.DarkForest);
    }
}