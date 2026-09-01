namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

/// <summary>Verifies Game.ToSnapshot()/FromSnapshot() round-trips faithfully (the mechanism Infrastructure relies on).</summary>
public class GameSnapshotRoundTripTests
{
    [Fact]
    public void RoundTrip_FreshlyCreatedGame_PreservesAllObservableState()
    {
        var original = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);

        var rehydrated = Game.FromSnapshot(original.ToSnapshot());

        rehydrated.Id.Should().Be(original.Id);
        rehydrated.AdventureId.Should().Be(AdventureWorldFactory.DefaultAdventureId);
        rehydrated.Player.CurrentLocationId.Should().Be(original.Player.CurrentLocationId);
        rehydrated.Puzzle.RequiredItemId.Should().Be(original.Puzzle.RequiredItemId);
        rehydrated.Locations.Should().HaveCount(original.Locations.Count);
        rehydrated.GetLocation(AdventureWorldFactory.OldBridge).ObjectIds.Should().Contain(AdventureWorldFactory.BridgeKey);
    }

    [Fact]
    public void FromSnapshot_WhenAdventureIdIsMissing_UsesDefaultAdventureId()
    {
        var original = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var snapshot = original.ToSnapshot() with { AdventureId = null };

        var rehydrated = Game.FromSnapshot(snapshot);

        rehydrated.AdventureId.Should().Be(AdventureWorldFactory.DefaultAdventureId);
    }

    [Fact]
    public void RoundTrip_AfterSeveralEventsApplied_PreservesInventoryFlagsAndEventHistory()
    {
        var original = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        original.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));
        original.Apply(new NpcRelationshipChangedEvent(
            Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.Hermit, WorldFlags.NpcGaveClue, "Hi", "Beware."));
        original.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.DarkForest, AdventureWorldFactory.OldBridge));
        original.Apply(new ItemTakenEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.BridgeKey, AdventureWorldFactory.OldBridge));

        var rehydrated = Game.FromSnapshot(original.ToSnapshot());

        rehydrated.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.OldBridge);
        rehydrated.Player.Inventory.Contains(AdventureWorldFactory.BridgeKey).Should().BeTrue();
        rehydrated.Player.KnownClues.Should().Contain(AdventureWorldFactory.TowerClueKey);
        rehydrated.HasFlag(WorldFlags.NpcGaveClue).Should().BeTrue();
        rehydrated.EventHistory.Should().HaveCount(original.EventHistory.Count);
        rehydrated.GetNpc(AdventureWorldFactory.Hermit).ConversationMemory.Should().ContainSingle();
    }

    [Fact]
    public void RoundTrip_AfterPuzzleSolved_PreservesSolvedStateAndUnlockedExit()
    {
        var original = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        original.Apply(new PuzzleSolvedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForgottenTowerEntrance));

        var rehydrated = Game.FromSnapshot(original.ToSnapshot());

        rehydrated.Puzzle.Solved.Should().BeTrue();
        rehydrated.HasFlag(WorldFlags.TowerUnlocked).Should().BeTrue();
    }
}
