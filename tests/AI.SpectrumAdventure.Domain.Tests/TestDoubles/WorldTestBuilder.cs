namespace AI.SpectrumAdventure.Domain.Tests.TestDoubles;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Worlds;

internal static class WorldTestBuilder
{
    internal static World Create(string seed = "test-seed")
    {
        var regionId = new RegionId("test-region");
        var start = new WorldLocation(new LocationId("test-start"), regionId, LocationType.Path, "test-start", "A test location.", new EnvironmentalProperties(TerrainKind.Forest, "temperate", "clear"));
        var region = new Region(regionId, RegionType.Wilderness, [TerrainKind.Forest], new RegionExpansionPolicy(4), [start.Id]);
        return new World(WorldId.New(), new WorldSeed(seed), new GenerationVersion("test-v1"), [region], [start], []);
    }
}