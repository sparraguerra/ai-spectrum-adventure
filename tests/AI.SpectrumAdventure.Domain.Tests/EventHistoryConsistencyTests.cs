namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

/// <summary>SC-008: across a 15+ turn session, no previously established fact is contradicted.</summary>
public class EventHistoryConsistencyTests
{
    [Fact]
    public void EventHistory_Across15PlusTurns_PreservesEveryEventInOrder_AndNeverMutatesPastEntries()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var start = DateTimeOffset.UtcNow;
        var events = new List<GameEvent>
        {
            new PlayerMovedEvent(Guid.NewGuid(), start, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest),
            new LocationDiscoveredEvent(Guid.NewGuid(), start.AddSeconds(1), AdventureWorldFactory.DarkForest),
            new PlayerMovedEvent(Guid.NewGuid(), start.AddSeconds(2), AdventureWorldFactory.DarkForest, AdventureWorldFactory.OldBridge),
            new LocationDiscoveredEvent(Guid.NewGuid(), start.AddSeconds(3), AdventureWorldFactory.OldBridge),
            new ItemTakenEvent(Guid.NewGuid(), start.AddSeconds(4), AdventureWorldFactory.BridgeKey, AdventureWorldFactory.OldBridge),
        };

        // Pad to 15+ turns with harmless back-and-forth movement that doesn't change any established fact.
        for (var i = 0; i < 10; i++)
        {
            events.Add(new PlayerMovedEvent(Guid.NewGuid(), start.AddSeconds(5 + i), AdventureWorldFactory.OldBridge, AdventureWorldFactory.DarkForest));
            events.Add(new PlayerMovedEvent(Guid.NewGuid(), start.AddSeconds(5.5 + i), AdventureWorldFactory.DarkForest, AdventureWorldFactory.OldBridge));
        }

        var snapshotBeforeReplay = events.ToArray();

        game.ApplyRange(events);

        game.EventHistory.Should().HaveCount(events.Count);
        game.EventHistory.Should().HaveCountGreaterOrEqualTo(15);
        game.EventHistory.Should().ContainInOrder(snapshotBeforeReplay);
        // The two earliest facts established (discovering Dark Forest, discovering Old Bridge) must still hold.
        game.GetLocation(AdventureWorldFactory.DarkForest).Discovered.Should().BeTrue();
        game.GetLocation(AdventureWorldFactory.OldBridge).Discovered.Should().BeTrue();
        game.Player.Inventory.Contains(AdventureWorldFactory.BridgeKey).Should().BeTrue();
    }
}
