namespace AI.SpectrumAdventure.Application.Worlds;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Worlds;

public sealed class WorldGenerationRules : IWorldGenerationRules
{
    public bool CanExpand(World world, LocationId sourceLocationId, ConnectionDirection direction)
    {
        var source = world.GetLocation(sourceLocationId);
        var region = world.Regions.Single(region => region.Id == source.RegionId);
        return world.FindConnection(sourceLocationId, direction) is null && Domain.Worlds.WorldGenerationRules.CanExpand(region, direction);
    }
}