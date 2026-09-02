namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Worlds;
using FluentAssertions;

public sealed class WorldConstraintValidatorTests
{
    [Fact]
    public void Validate_RejectsInvalidTerrainWithoutMutatingWorld()
    {
        var world = CreateWorld();
        var candidate = CreateCandidate(world, new EnvironmentalProperties(TerrainKind.Water, "temperate", "daylight"));

        var result = new WorldConstraintValidator().Validate(world, candidate);

        result.IsValid.Should().BeFalse();
        world.Locations.Should().HaveCount(2);
        world.Events.Should().BeEmpty();
    }

    [Fact]
    public void Validate_RejectsDuplicateLocationIdentity()
    {
        var world = CreateWorld();
        var candidate = CreateCandidate(world, new EnvironmentalProperties(TerrainKind.Forest, "temperate", "daylight"), new LocationId("existing"));

        new WorldConstraintValidator().Validate(world, candidate).IsValid.Should().BeFalse();
    }

    private static World CreateWorld()
    {
        var regionId = new RegionId("region");
        var start = new WorldLocation(new LocationId("start"), regionId, LocationType.Path, "start", "A path.", new EnvironmentalProperties(TerrainKind.Forest, "temperate", "daylight"));
        var existing = new WorldLocation(new LocationId("existing"), regionId, LocationType.Path, "existing", "An existing path.", new EnvironmentalProperties(TerrainKind.Forest, "temperate", "daylight"));
        return new World(WorldId.New(), new WorldSeed("seed"), new GenerationVersion("1"), [new Region(regionId, RegionType.Wilderness, [TerrainKind.Forest], new RegionExpansionPolicy(3, new HashSet<ConnectionDirection>(Enum.GetValues<ConnectionDirection>())), [start.Id, existing.Id])], [start, existing], []);
    }

    private static WorldExpansionCandidate CreateCandidate(World world, EnvironmentalProperties environment, LocationId? id = null)
    {
        var location = new WorldLocation(id ?? new LocationId("new"), new RegionId("region"), LocationType.Path, "new", "A new path.", environment);
        var key = new GenerationKey("key");
        return new WorldExpansionCandidate(null, location, new WorldConnection(new ConnectionId("connection"), new LocationId("start"), location.Id, ConnectionDirection.South), new GeneratedContentMetadata(key, world.Seed, world.GenerationVersion, new LocationId("start"), ConnectionDirection.South, 0, "test", DateTimeOffset.UnixEpoch));
    }
}