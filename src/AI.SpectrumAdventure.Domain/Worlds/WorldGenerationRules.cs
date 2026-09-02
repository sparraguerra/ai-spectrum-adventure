namespace AI.SpectrumAdventure.Domain.Worlds;

public static class WorldGenerationRules
{
    public static bool IsTerrainTransitionPermitted(TerrainKind source, TerrainKind destination) => source == destination || (source, destination) switch
    {
        (TerrainKind.Forest, TerrainKind.Hills or TerrainKind.Plains or TerrainKind.Marsh) => true,
        (TerrainKind.Hills, TerrainKind.Forest or TerrainKind.Plains or TerrainKind.Stone) => true,
        (TerrainKind.Marsh, TerrainKind.Forest or TerrainKind.Plains or TerrainKind.Water) => true,
        (TerrainKind.Plains, TerrainKind.Forest or TerrainKind.Hills or TerrainKind.Marsh or TerrainKind.Water) => true,
        (TerrainKind.Stone, TerrainKind.Hills or TerrainKind.Forest) => true,
        (TerrainKind.Water, TerrainKind.Marsh or TerrainKind.Plains) => true,
        _ => false,
    };

    public static bool CanExpand(Region region, ConnectionDirection direction) =>
        region.ExpansionPolicy.AllowedDirections.Contains(direction) &&
        region.LocationIds.Count < region.ExpansionPolicy.MaximumExpansions + 1;
}