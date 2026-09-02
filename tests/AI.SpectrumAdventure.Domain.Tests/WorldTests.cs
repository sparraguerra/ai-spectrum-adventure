namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Worlds;
using FluentAssertions;

public class WorldTests
{
    [Fact]
    public void Constructor_PreservesWorldIdentityAndGenerationMetadata()
    {
        var world = CreateWorld();

        world.Id.Should().Be(new WorldId(Guid.Parse("11111111-1111-1111-1111-111111111111")));
        world.Seed.Should().Be(new WorldSeed("seed"));
        world.GenerationVersion.Should().Be(new GenerationVersion("v1"));
    }

    [Fact]
    public void Apply_AppendsValidatedEventsInSequence()
    {
        var world = CreateWorld();
        var eventId = new WorldEventId(Guid.NewGuid());
        var change = new ConnectionStateChangedWorldEvent(eventId, 1, DateTimeOffset.UtcNow, WorldEvent.FormatCause(WorldEventCause.WorldRule, "bridge-collapsed"), new ConnectionId("path"), ConnectionVisibility.Discovered, ConnectionAvailability.Blocked);

        world.Apply(change);

        world.Events.Should().ContainSingle().Which.Should().BeSameAs(change);
        world.FindConnection(new LocationId("start"), ConnectionDirection.North)!.Availability.Should().Be(ConnectionAvailability.Blocked);
        Action applyAgain = () => world.Apply(change);
        applyAgain.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_RejectsDuplicateSourceDirectionConnections()
    {
        var start = CreateLocation("start");
        var north = CreateLocation("north");
        var east = CreateLocation("east");
        Action create = () => new World(new WorldId(Guid.NewGuid()), new WorldSeed("seed"), new GenerationVersion("v1"), [CreateRegion("start", "north", "east")], [start, north, east], [new WorldConnection(new ConnectionId("one"), start.Id, north.Id, ConnectionDirection.North), new WorldConnection(new ConnectionId("two"), start.Id, east.Id, ConnectionDirection.North)]);

        create.Should().Throw<InvalidOperationException>().WithMessage("*one connection in each direction*");
    }

    [Fact]
    public void Apply_RejectsRepeatedGenerationKey()
    {
        var world = CreateWorld();
        world.Apply(CreateExpansionEvent(1, "destination", "generated", "connection"));

        Action applyAgain = () => world.Apply(CreateExpansionEvent(2, "another", "generated", "other-connection"));

        applyAgain.Should().Throw<InvalidOperationException>().WithMessage("*Generation key*");
        world.Locations.Should().HaveCount(3);
    }

    [Fact]
    public void Constructor_ReplaysConnectionChangeEventAfterRestoringBaseGraph()
    {
        var world = CreateWorld();
        var change = new ConnectionStateChangedWorldEvent(
            new WorldEventId(Guid.NewGuid()), 1, DateTimeOffset.UtcNow, WorldEvent.FormatCause(WorldEventCause.WorldRule, "bridge-collapsed"),
            new ConnectionId("path"), ConnectionVisibility.Discovered, ConnectionAvailability.Blocked);

        var rehydrated = new World(world.Id, world.Seed, world.GenerationVersion, world.Regions, world.Locations,
            world.Connections, world.GenerationMetadata, [change]);

        rehydrated.FindConnection(new LocationId("start"), ConnectionDirection.North)!.Availability
            .Should().Be(ConnectionAvailability.Blocked);
        rehydrated.Events.Should().ContainSingle().Which.Should().Be(change);
    }

    private static World CreateWorld() => new(new WorldId(Guid.Parse("11111111-1111-1111-1111-111111111111")), new WorldSeed("seed"), new GenerationVersion("v1"), [CreateRegion("start", "north")], [CreateLocation("start"), CreateLocation("north")], [new WorldConnection(new ConnectionId("path"), new LocationId("start"), new LocationId("north"), ConnectionDirection.North)]);
    private static Region CreateRegion(params string[] locations) => new(new RegionId("region"), RegionType.Wilderness, [TerrainKind.Forest], new RegionExpansionPolicy(3), locations.Select(id => new LocationId(id)));
    private static WorldLocation CreateLocation(string id) => new(new LocationId(id), new RegionId("region"), LocationType.Path, id, "A forest path.", new EnvironmentalProperties(TerrainKind.Forest, "temperate", "dim"));
    private static WorldExpandedEvent CreateExpansionEvent(long sequence, string locationId, string generationKey, string connectionId) => new(new WorldEventId(Guid.NewGuid()), sequence, DateTimeOffset.UtcNow, WorldEvent.FormatCause(WorldEventCause.PlayerAction, "exploration"), null, CreateLocation(locationId), new WorldConnection(new ConnectionId(connectionId), new LocationId("start"), new LocationId(locationId), ConnectionDirection.East), new GeneratedContentMetadata(new GenerationKey(generationKey), new WorldSeed("seed"), new GenerationVersion("v1"), new LocationId("start"), ConnectionDirection.East, 0, "exploration", DateTimeOffset.UtcNow));
}