namespace AI.SpectrumAdventure.Domain.Worlds;

using System.Text.Json.Serialization;
using AI.SpectrumAdventure.Domain.Common;

public sealed class Region
{
    public RegionId Id { get; }
    public RegionType Type { get; }
    public IReadOnlyCollection<TerrainKind> TerrainProfile { get; }
    public RegionExpansionPolicy ExpansionPolicy { get; }
    public HashSet<LocationId> LocationIds { get; }

    public Region(RegionId id, RegionType type, IReadOnlyCollection<TerrainKind> terrainProfile, RegionExpansionPolicy expansionPolicy, IEnumerable<LocationId>? locationIds = null)
        : this(id, type, terrainProfile, expansionPolicy, locationIds?.ToHashSet())
    {
    }

    [JsonConstructor]
    public Region(RegionId id, RegionType type, IReadOnlyCollection<TerrainKind> terrainProfile, RegionExpansionPolicy expansionPolicy, HashSet<LocationId>? locationIds)
    {
        var terrain = terrainProfile?.Distinct().ToArray() ?? throw new ArgumentNullException(nameof(terrainProfile));
        if (terrain.Length == 0) throw new ArgumentException("A region needs at least one terrain type.", nameof(terrainProfile));

        Id = id;
        Type = type;
        TerrainProfile = terrain;
        ExpansionPolicy = expansionPolicy ?? throw new ArgumentNullException(nameof(expansionPolicy));
        LocationIds = locationIds is null ? [] : [.. locationIds];
    }

    internal void AddLocation(LocationId locationId) => LocationIds.Add(locationId);
}