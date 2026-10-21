namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Application.Rules;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

public class BackNavigationTests
{
    private static ParsedIntent BackIntent() =>
        new(IntentAction.Back, null, new Dictionary<string, string>(), 1.0, "back");

    private static ParsedIntent GoIntent(string direction) =>
        new(IntentAction.Go, direction, new Dictionary<string, string>(), 1.0, $"go {direction}");

    [Fact]
    public void ProcessAction_Back_ReturnsThroughSuccessfulMovesInReverseOrder()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        AdventureOrchestrator.ProcessAction(game, GoIntent("north"));
        AdventureOrchestrator.ProcessAction(game, GoIntent("east"));

        var firstBack = AdventureOrchestrator.ProcessAction(game, BackIntent());
        var secondBack = AdventureOrchestrator.ProcessAction(game, BackIntent());

        firstBack.Success.Should().BeTrue();
        firstBack.CurrentLocationId.Should().Be(AdventureWorldFactory.DarkForest.Value);
        secondBack.Success.Should().BeTrue();
        secondBack.CurrentLocationId.Should().Be(AdventureWorldFactory.ForestEntrance.Value);
    }

    [Fact]
    public void ProcessAction_BackWithoutHistory_LeavesPlayerInPlaceAndExplainsWhy()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var startingLocation = game.Player.CurrentLocationId.Value;

        var result = AdventureOrchestrator.ProcessAction(game, BackIntent());

        result.Success.Should().BeFalse();
        result.Reason.Should().Be(nameof(RulesFailureReason.NoPreviousLocation));
        result.CurrentLocationId.Should().Be(startingLocation);
        result.Narrative.Should().Contain("previous");
        game.Player.PreviousLocationIds.Should().BeEmpty();
    }

    [Fact]
    public void ProcessAction_FailedMovementDoesNotCreateHistoryForBack()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var startingLocation = game.Player.CurrentLocationId.Value;

        var failedMove = AdventureOrchestrator.ProcessAction(game, GoIntent("south"));
        var back = AdventureOrchestrator.ProcessAction(game, BackIntent());

        failedMove.Success.Should().BeFalse();
        back.Success.Should().BeFalse();
        back.Reason.Should().Be(nameof(RulesFailureReason.NoPreviousLocation));
        back.CurrentLocationId.Should().Be(startingLocation);
    }
}
