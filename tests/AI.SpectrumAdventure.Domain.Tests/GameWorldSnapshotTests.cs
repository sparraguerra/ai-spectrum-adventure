namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;

public class GameWorldSnapshotTests
{
    [Fact]
    public void RoundTrip_WithWorldAndKnowledge_PreservesBoth()
    {
        var worldId = new WorldId(Guid.NewGuid());
        var knowledge = new PlayerKnowledge();
        var discoveredAt = DateTimeOffset.UtcNow;
        knowledge.DiscoverLocation(new LocationId("forest"), discoveredAt);
        knowledge.DiscoverLore(new LoreId("old-king"), discoveredAt);
        var game = CreateGame(worldId, knowledge);

        var rehydrated = Game.FromSnapshot(game.ToSnapshot());

        rehydrated.WorldId.Should().Be(worldId);
        rehydrated.PlayerKnowledge.Locations.Should().ContainKey("forest").WhoseValue.Should().Be(discoveredAt);
        rehydrated.PlayerKnowledge.Lore.Should().ContainKey("old-king").WhoseValue.Should().Be(discoveredAt);
    }

    [Fact]
    public void FromSnapshot_WithoutWorldFields_PreservesFeatureOneState()
    {
        var original = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var legacySnapshot = original.ToSnapshot() with { WorldId = null, PlayerKnowledge = null };

        var rehydrated = Game.FromSnapshot(legacySnapshot);

        rehydrated.WorldId.Should().BeNull();
        rehydrated.PlayerKnowledge.Locations.Should().BeEmpty();
        rehydrated.Player.CurrentLocationId.Should().Be(original.Player.CurrentLocationId);
        rehydrated.Locations.Should().HaveCount(original.Locations.Count);
    }

    private static Game CreateGame(WorldId worldId, PlayerKnowledge knowledge)
    {
        var baseGame = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        return new Game(baseGame.Id, baseGame.CreatedAt, baseGame.Player, baseGame.Locations, baseGame.Items, baseGame.Npcs, baseGame.Puzzle, baseGame.AdventureId, worldId, knowledge);
    }
}