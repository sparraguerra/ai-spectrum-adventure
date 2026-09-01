namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Rules;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Items;
using FluentAssertions;
using Xunit;

public class ObjectRulesTests
{
    private static Game CreateGame() => AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);

    [Fact]
    public void Validate_VisibleObjectInCurrentLocation_Succeeds()
    {
        var game = CreateGame();

        var result = ObjectRules.Validate(AdventureWorldFactory.Sign, game);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Validate_ObjectNotInCurrentLocationOrInventory_FailsWithTargetNotPresent()
    {
        var game = CreateGame();

        var result = ObjectRules.Validate(AdventureWorldFactory.BridgeKey, game);

        result.Success.Should().BeFalse();
        result.Reason.Should().Be(RulesFailureReason.TargetNotPresent);
    }

    [Fact]
    public void Validate_HiddenObjectWithoutRevealCondition_FailsWithTargetNotPresent()
    {
        var hiddenItem = new GameItem(new ItemId("hidden-thing"), "Hidden Thing", "desc", ItemState.Hidden, AdventureWorldFactory.ForestEntrance);
        var game = new Game(
            GameId.New(),
            DateTimeOffset.UtcNow,
            new Domain.Players.Player(AdventureWorldFactory.ForestEntrance),
            [new Domain.Locations.Location(AdventureWorldFactory.ForestEntrance, "Forest Entrance", "desc", objectIds: [hiddenItem.Id])],
            [hiddenItem],
            [],
            new Domain.Puzzles.Puzzle(AdventureWorldFactory.ForgottenTowerEntrance, new Domain.Puzzles.PuzzleSolutionCondition(AdventureWorldFactory.BridgeKey, "tower-clue")));

        var result = ObjectRules.Validate(hiddenItem.Id, game);

        result.Success.Should().BeFalse();
        result.Reason.Should().Be(RulesFailureReason.TargetNotPresent);
    }
}
