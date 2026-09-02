namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Items;
using AI.SpectrumAdventure.Domain.Worlds;
using FluentAssertions;

public sealed class WorldEvolutionTests
{
    [Fact]
    public void Apply_StateChangingEventsPreserveLocationIdentityAndRequireAnExplicitConnectionEventToReopen()
    {
        var world = CreateWorld();
        var originalLocation = world.GetLocation(new LocationId("north"));

        world.Apply(new LocationStateChangedWorldEvent(WorldEventId.New(), 1, DateTimeOffset.UnixEpoch, "WorldRule:fire", originalLocation.Id, LocationState.Altered));
        world.Apply(new ObjectStateChangedWorldEvent(WorldEventId.New(), 2, DateTimeOffset.UnixEpoch, "PlayerAction:opened-chest", new ItemId("chest"), ItemState.Visible));
        world.Apply(new NpcMovedWorldEvent(WorldEventId.New(), 3, DateTimeOffset.UnixEpoch, "TurnElapsed:patrol", new NpcId("warden"), originalLocation.Id));
        world.Apply(new PuzzleStateChangedWorldEvent(WorldEventId.New(), 4, DateTimeOffset.UnixEpoch, "PuzzleOutcome:lever", new PuzzleId("lever"), true));
        world.Apply(new ConnectionStateChangedWorldEvent(WorldEventId.New(), 5, DateTimeOffset.UnixEpoch, "WorldRule:collapse", new ConnectionId("path"), ConnectionVisibility.Discovered, ConnectionAvailability.Blocked));

        world.GetLocation(originalLocation.Id).Should().BeSameAs(originalLocation);
        world.GetLocation(originalLocation.Id).State.Should().Be(LocationState.Altered);
        world.ObjectStates[new ItemId("chest")].Should().Be(ItemState.Visible);
        world.NpcLocations[new NpcId("warden")].Should().Be(originalLocation.Id);
        world.PuzzleStates[new PuzzleId("lever")].Should().BeTrue();
        world.FindConnection(new LocationId("start"), ConnectionDirection.North)!.Availability.Should().Be(ConnectionAvailability.Blocked);

        world.Apply(new ConnectionStateChangedWorldEvent(WorldEventId.New(), 6, DateTimeOffset.UnixEpoch, "WorldRule:cleared", new ConnectionId("path"), ConnectionVisibility.Discovered, ConnectionAvailability.Available));

        world.FindConnection(new LocationId("start"), ConnectionDirection.North)!.Availability.Should().Be(ConnectionAvailability.Available);
    }

    private static World CreateWorld()
    {
        var region = new Region(new RegionId("region"), RegionType.Wilderness, [TerrainKind.Forest], new RegionExpansionPolicy(3), [new LocationId("start"), new LocationId("north")]);
        var locations = new[]
        {
            new WorldLocation(new LocationId("start"), region.Id, LocationType.Path, "start", "A forest path.", new EnvironmentalProperties(TerrainKind.Forest, "temperate", "dim")),
            new WorldLocation(new LocationId("north"), region.Id, LocationType.Path, "north", "A northern path.", new EnvironmentalProperties(TerrainKind.Forest, "temperate", "dim")),
        };
        return new World(WorldId.New(), new WorldSeed("seed"), new GenerationVersion("v1"), [region], locations, [new WorldConnection(new ConnectionId("path"), locations[0].Id, locations[1].Id, ConnectionDirection.North, ConnectionVisibility.Discovered)]);
    }
}