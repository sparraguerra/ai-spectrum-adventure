namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Items;
using FluentAssertions;
using Xunit;

public class GameEventApplicationTests
{
    private static Game CreateGame() => AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);

    [Fact]
    public void Apply_PlayerMoved_UpdatesCurrentLocation()
    {
        var game = CreateGame();
        var now = DateTimeOffset.UtcNow;

        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), now, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));

        game.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.DarkForest);
    }

    [Fact]
    public void Apply_ItemTaken_MovesItemFromLocationToInventory()
    {
        var game = CreateGame();
        var now = DateTimeOffset.UtcNow;

        game.Apply(new ItemTakenEvent(Guid.NewGuid(), now, AdventureWorldFactory.BridgeKey, AdventureWorldFactory.OldBridge));

        game.Player.Inventory.Contains(AdventureWorldFactory.BridgeKey).Should().BeTrue();
        game.GetLocation(AdventureWorldFactory.OldBridge).ObjectIds.Should().NotContain(AdventureWorldFactory.BridgeKey);
        game.GetItem(AdventureWorldFactory.BridgeKey).LocationId.Should().BeNull();
    }

    [Fact]
    public void Apply_ObjectStateChanged_UpdatesItemState()
    {
        var game = CreateGame();

        game.Apply(new ObjectStateChangedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.Sign, ItemState.Visible));

        game.GetItem(AdventureWorldFactory.Sign).State.Should().Be(ItemState.Visible);
    }

    [Fact]
    public void Apply_NpcRelationshipChanged_WithClueFlag_GrantsPlayerTheClue()
    {
        var game = CreateGame();

        game.Apply(new NpcRelationshipChangedEvent(
            Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.Hermit, WorldFlags.NpcGaveClue,
            "What do you know of the tower?", "Beware the door that only opens for those who ask kindly."));

        game.Player.KnownClues.Should().Contain(AdventureWorldFactory.TowerClueKey);
        game.GetNpc(AdventureWorldFactory.Hermit).ConversationMemory.Should().ContainSingle();
        game.HasFlag(WorldFlags.NpcGaveClue).Should().BeTrue();
    }

    [Fact]
    public void Apply_PuzzleSolved_MarksPuzzleSolvedAndSetsTowerUnlockedFlag()
    {
        var game = CreateGame();

        game.Apply(new PuzzleSolvedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForgottenTowerEntrance));

        game.Puzzle.Solved.Should().BeTrue();
        game.HasFlag(WorldFlags.TowerUnlocked).Should().BeTrue();
    }

    [Fact]
    public void Apply_LocationDiscovered_MarksLocationDiscovered()
    {
        var game = CreateGame();

        game.Apply(new LocationDiscoveredEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.DarkForest));

        game.GetLocation(AdventureWorldFactory.DarkForest).Discovered.Should().BeTrue();
    }

    [Fact]
    public void EventHistory_IsAppendOnly_AndPreservesOrder()
    {
        var game = CreateGame();
        var first = new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest);
        var second = new LocationDiscoveredEvent(Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(1), AdventureWorldFactory.DarkForest);

        game.Apply(first);
        game.Apply(second);

        game.EventHistory.Should().HaveCount(2);
        game.EventHistory.ElementAt(0).Should().Be(first);
        game.EventHistory.ElementAt(1).Should().Be(second);
    }

    [Fact]
    public void ApplyRange_AppliesAllEventsInOrder()
    {
        var game = CreateGame();
        var now = DateTimeOffset.UtcNow;

        game.ApplyRange(
        [
            new PlayerMovedEvent(Guid.NewGuid(), now, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest),
            new ItemTakenEvent(Guid.NewGuid(), now.AddSeconds(1), AdventureWorldFactory.BridgeKey, AdventureWorldFactory.OldBridge),
        ]);

        game.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.DarkForest);
        game.Player.Inventory.Contains(AdventureWorldFactory.BridgeKey).Should().BeTrue();
        game.EventHistory.Should().HaveCount(2);
    }
}
