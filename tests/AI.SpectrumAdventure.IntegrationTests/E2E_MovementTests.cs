namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;

public sealed class E2E_MovementTests
{
    [Fact]
    public void Movement_UpdatesLocationAndNarrative()
    {
        var game = E2ETestSupport.CreateStartedGame();

        var result = AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Go, "north"));

        result.Success.Should().BeTrue();
        result.SceneChanged.Should().BeTrue();
        result.CurrentLocationId.Should().Be(AdventureWorldFactory.DarkForest.Value);
        result.Narrative.Should().Contain("ancient trees");
    }
}