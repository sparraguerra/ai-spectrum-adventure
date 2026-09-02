namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Worlds;
using FluentAssertions;

public sealed class WorldEventInvariantTests
{
    [Fact]
    public void Apply_RejectsAnEventWithoutADeterministicCause()
    {
        var world = CreateWorld();
        var action = () => world.Apply(new ConnectionStateChangedWorldEvent(WorldEventId.New(), 1, DateTimeOffset.UnixEpoch, "collapse", new ConnectionId("path"), ConnectionVisibility.Discovered, ConnectionAvailability.Blocked));

        action.Should().Throw<InvalidOperationException>();
        world.Events.Should().BeEmpty();
    }

    [Fact]
    public void Apply_RejectsReopeningABlockedPathWithoutAnExplicitPermittedTransition()
    {
        var world = CreateWorld();
        world.Apply(new ConnectionStateChangedWorldEvent(WorldEventId.New(), 1, DateTimeOffset.UnixEpoch, "WorldRule:collapse", new ConnectionId("path"), ConnectionVisibility.Discovered, ConnectionAvailability.Blocked));

        var action = () => world.Apply(new ConnectionStateChangedWorldEvent(WorldEventId.New(), 2, DateTimeOffset.UnixEpoch, "WorldRule:weather", new ConnectionId("path"), ConnectionVisibility.Discovered, ConnectionAvailability.Available));

        action.Should().Throw<InvalidOperationException>();
        world.FindConnection(new LocationId("start"), ConnectionDirection.North)!.Availability.Should().Be(ConnectionAvailability.Blocked);
    }

    private static World CreateWorld()
    {
        var regionId = new RegionId("region");
        var start = new WorldLocation(new LocationId("start"), regionId, LocationType.Path, "start", "A path.", new EnvironmentalProperties(TerrainKind.Forest, "temperate", "daylight"));
        var north = new WorldLocation(new LocationId("north"), regionId, LocationType.Path, "north", "A northern path.", new EnvironmentalProperties(TerrainKind.Forest, "temperate", "daylight"));
        var region = new Region(regionId, RegionType.Wilderness, [TerrainKind.Forest], new RegionExpansionPolicy(3), [start.Id, north.Id]);
        return new World(WorldId.New(), new WorldSeed("seed"), new GenerationVersion("v1"), [region], [start, north], [new WorldConnection(new ConnectionId("path"), start.Id, north.Id, ConnectionDirection.North, ConnectionVisibility.Discovered)]);
    }
}