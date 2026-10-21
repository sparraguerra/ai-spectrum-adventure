namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

public class NavigationHistoryTests
{
    [Fact]
    public void Apply_OrdinaryMovement_RecordsOriginAndBacktrackingConsumesIt()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var origin = game.Player.CurrentLocationId;
        var destination = AdventureWorldFactory.DarkForest;

        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, origin, destination));

        game.Player.CurrentLocationId.Should().Be(destination);
        game.Player.PreviousLocationIds.Should().ContainSingle().Which.Should().Be(origin);

        game.Apply(new PlayerBacktrackedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, destination, origin));

        game.Player.CurrentLocationId.Should().Be(origin);
        game.Player.PreviousLocationIds.Should().BeEmpty();
    }

    [Fact]
    public void Apply_MultipleOrdinaryMovements_BacktrackingPopsHistoryInReverseOrder()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var start = game.Player.CurrentLocationId;
        var middle = AdventureWorldFactory.DarkForest;
        var destination = AdventureWorldFactory.OldBridge;

        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, start, middle));
        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, middle, destination));

        game.Player.PreviousLocationIds.Should().Equal(start, middle);

        game.Apply(new PlayerBacktrackedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, destination, middle));
        game.Apply(new PlayerBacktrackedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, middle, start));

        game.Player.CurrentLocationId.Should().Be(start);
        game.Player.PreviousLocationIds.Should().BeEmpty();
    }

    [Fact]
    public void NewGame_StartsWithEmptyPreviousLocationHistory()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);

        game.Player.PreviousLocationIds.Should().BeEmpty();
    }
}
