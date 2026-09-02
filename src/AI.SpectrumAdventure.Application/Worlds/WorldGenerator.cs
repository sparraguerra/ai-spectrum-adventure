namespace AI.SpectrumAdventure.Application.Worlds;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Worlds;

public sealed class WorldGenerator : IRegionGenerator, ILocationGenerator, IConnectionGenerator
{
    public Region? Generate(WorldGenerationRequest request, GenerationKey generationKey)
    {
        var source = request.World.GetLocation(request.SourceLocationId);
        return new Region(
            new RegionId($"generated-region-{generationKey.Value[..12]}"),
            RegionType.Wilderness,
            [source.Environment.Terrain],
            new RegionExpansionPolicy(3, new HashSet<ConnectionDirection>(Enum.GetValues<ConnectionDirection>())));
    }

    public WorldLocation Generate(WorldGenerationRequest request, GenerationKey generationKey, Region? region)
    {
        var source = request.World.GetLocation(request.SourceLocationId);
        var terrain = source.Environment.Terrain;
        var names = new[] { "quiet-path", "weathered-clearing", "stone-marker", "hidden-glen" };
        var name = names[DeterministicGenerationKeyFactory.SelectIndex(generationKey, names.Length)];
        var id = new LocationId($"generated-{generationKey.Value[..12]}");
        return new WorldLocation(id, region?.Id ?? source.RegionId, LocationType.Path, name, $"A {name.Replace('-', ' ')} extends from the familiar route.", new EnvironmentalProperties(terrain, source.Environment.Climate, source.Environment.Lighting, source.Environment.Features));
    }

    public WorldConnection Generate(WorldGenerationRequest request, GenerationKey generationKey, WorldLocation location) =>
        new(new ConnectionId($"{request.SourceLocationId.Value}:{request.Direction}:{generationKey.Value[..12]}"), request.SourceLocationId, location.Id, request.Direction, ConnectionVisibility.Discovered);
}