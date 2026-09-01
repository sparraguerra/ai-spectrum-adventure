namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Agents.Narrator;
using AI.SpectrumAdventure.Agents.Tests.TestDoubles;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

public class NarratorFailureFallbackTests
{
    [Fact]
    public async Task ProcessActionWithNarrationAsync_NarratorAgentFailsTwice_GameplayContinuesWithDeterministicNarrative()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var failingRunner = new FakeAgentRunner()
            .EnqueueFailure(new InvalidOperationException("simulated model outage"))
            .EnqueueFailure(new InvalidOperationException("simulated model outage"));
        var narrator = new NarratorAgent(failingRunner);
        var intent = new ParsedIntent(IntentAction.Go, "North", new Dictionary<string, string>(), 1.0, "go north");

        var result = await AdventureOrchestrator.ProcessActionWithNarrationAsync(game, intent, narrator);

        result.Success.Should().BeTrue();
        result.CurrentLocationId.Should().Be(AdventureWorldFactory.DarkForest.Value);
        result.Narrative.Should().NotBeNullOrWhiteSpace();
        game.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.DarkForest);
    }
}
