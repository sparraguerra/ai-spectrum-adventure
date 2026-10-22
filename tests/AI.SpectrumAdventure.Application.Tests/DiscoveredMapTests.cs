namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;
using FluentAssertions;

public sealed class DiscoveredMapTests
{
    [Fact]
    public void Execute_ExcludesUndiscoveredWorldNodesAndEdges()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UnixEpoch);
        var world = WorldBootstrapFactory.Create(game, WorldId.New());
        game.BindWorld(world.Id);
        var start = world.GetLocation(game.Player.CurrentLocationId);
        game.PlayerKnowledge.DiscoverRegion(start.RegionId, DateTimeOffset.UnixEpoch);
        game.PlayerKnowledge.DiscoverLocation(start.Id, DateTimeOffset.UnixEpoch);

        var map = GetDiscoveredMapQuery.Execute(game, world);

        map.Locations.Should().ContainSingle(location => location.Id == start.Id.Value);
        map.Connections.Should().BeEmpty();
    }

    [Fact]
    public void Execute_IncludesConfirmedConnectionAndItsEndpointsWithCurrentAvailability()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UnixEpoch);
        var world = WorldBootstrapFactory.Create(game, WorldId.New());
        game.BindWorld(world.Id);
        var connection = world.Connections.First();
        game.PlayerKnowledge.DiscoverRegion(world.GetLocation(connection.SourceLocationId).RegionId, DateTimeOffset.UnixEpoch);
        game.PlayerKnowledge.DiscoverLocation(connection.SourceLocationId, DateTimeOffset.UnixEpoch);
        game.PlayerKnowledge.DiscoverLocation(connection.DestinationLocationId, DateTimeOffset.UnixEpoch);
        game.PlayerKnowledge.DiscoverConnection(connection.Id, DateTimeOffset.UnixEpoch);
        world.Apply(new ConnectionStateChangedWorldEvent(WorldEventId.New(), world.Events.Count + 1, DateTimeOffset.UnixEpoch, "WorldRule:collapse", connection.Id, connection.Visibility, ConnectionAvailability.Blocked));

        var map = GetDiscoveredMapQuery.Execute(game, world);

        var path = map.Connections.Should().ContainSingle(path => path.Id == connection.Id.Value && path.Availability == ConnectionAvailability.Blocked).Subject;
        path.SourceLocationName.Should().Be(world.GetLocation(connection.SourceLocationId).StructuralNameKey.Replace('-', ' '));
        path.DestinationLocationName.Should().Be(world.GetLocation(connection.DestinationLocationId).StructuralNameKey.Replace('-', ' '));
    }

    [Fact]
    public void Execute_IncludesLocationsAndPathsDiscoveredByStandardMovement()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UnixEpoch);
        var world = WorldBootstrapFactory.Create(game, WorldId.New());
        game.BindWorld(world.Id);
        game.Apply(new LocationDiscoveredEvent(Guid.NewGuid(), DateTimeOffset.UnixEpoch, game.Player.CurrentLocationId));

        AdventureOrchestrator.ProcessAction(game, new ParsedIntent(IntentAction.Go, "North", new Dictionary<string, string>(), 1.0, "north")).Success.Should().BeTrue();
        AdventureOrchestrator.ProcessAction(game, new ParsedIntent(IntentAction.Go, "East", new Dictionary<string, string>(), 1.0, "east")).Success.Should().BeTrue();

        var map = GetDiscoveredMapQuery.Execute(game, world);

        map.Locations.Select(location => location.Id).Should().Contain([AdventureWorldFactory.ForestEntrance.Value, AdventureWorldFactory.DarkForest.Value, AdventureWorldFactory.OldBridge.Value]);
        map.Connections.Should().Contain(path => path.SourceLocationId == AdventureWorldFactory.ForestEntrance.Value && path.DestinationLocationId == AdventureWorldFactory.DarkForest.Value);
        map.Connections.Should().Contain(path => path.SourceLocationId == AdventureWorldFactory.DarkForest.Value && path.DestinationLocationId == AdventureWorldFactory.OldBridge.Value);
    }

    [Fact]
    public void Select_TurnBasedEventsUseStableConnectionOrdering()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UnixEpoch);
        var world = WorldBootstrapFactory.Create(game, WorldId.New());
        var scheduler = new WorldEventScheduler();

        var selected = scheduler.Select(world, new WorldEventScheduleRequest(WorldEventCause.TurnElapsed, "storm", 0, DateTimeOffset.UnixEpoch));

        selected.Should().BeOfType<ConnectionStateChangedWorldEvent>();
        ((ConnectionStateChangedWorldEvent)selected!).ConnectionId.Should().Be(world.Connections.OrderBy(connection => connection.Id.Value, StringComparer.Ordinal).First().Id);
    }

    [Fact]
    public void Select_ActionTriggeredBlockTargetsOnlyTheNamedAvailableConnection()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UnixEpoch);
        var world = WorldBootstrapFactory.Create(game, WorldId.New());
        var connection = world.Connections.First();

        var selected = new WorldEventScheduler().Select(world, new WorldEventScheduleRequest(WorldEventCause.PlayerAction, $"block:{connection.Id.Value}", 0, DateTimeOffset.UnixEpoch));

        selected.Should().BeOfType<ConnectionStateChangedWorldEvent>();
        ((ConnectionStateChangedWorldEvent)selected!).ConnectionId.Should().Be(connection.Id);
    }
}