namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;

public sealed class E2E_InvalidMovementTests
{
    [Fact]
    public void InvalidMovement_LeavesStateUnchangedWithMeaningfulFeedback()
    {
        var game = E2ETestSupport.CreateStartedGame();

        var result = AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Go, "south"));

        result.Success.Should().BeFalse();
        result.Reason.Should().Be("NoSuchExit");
        game.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.ForestEntrance);
        result.Narrative.Should().Contain("no path");
    }
}