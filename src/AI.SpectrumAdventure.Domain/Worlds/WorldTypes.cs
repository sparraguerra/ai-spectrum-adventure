namespace AI.SpectrumAdventure.Domain.Worlds;

public enum RegionType
{
    Wilderness,
    Settlement,
    Ruins,
    Coast,
    Underground
}

public enum TerrainKind
{
    Forest,
    Hills,
    Marsh,
    Plains,
    Stone,
    Water
}

public enum LocationType
{
    Landmark,
    Path,
    Settlement,
    Interior,
    Ruin,
    Cavern
}

public enum ConnectionDirection
{
    North,
    East,
    South,
    West,
    Up,
    Down
}

public enum ConnectionVisibility
{
    Hidden,
    Discovered
}

public enum ConnectionAvailability
{
    Available,
    Blocked
}

public enum LocationState
{
    Normal,
    Altered,
    Destroyed
}

public sealed record EnvironmentalProperties(
    TerrainKind Terrain,
    string Climate,
    string Lighting,
    IReadOnlyCollection<string>? Features = null)
{
    public IReadOnlyCollection<string> Features { get; init; } = Features ?? [];
}

public sealed record RegionExpansionPolicy
{
    public int MaximumExpansions { get; }
    public HashSet<ConnectionDirection> AllowedDirections { get; }

    public RegionExpansionPolicy(int maximumExpansions, IEnumerable<ConnectionDirection>? allowedDirections = null)
        : this(maximumExpansions, allowedDirections?.ToHashSet())
    {
    }

    [System.Text.Json.Serialization.JsonConstructor]
    public RegionExpansionPolicy(int maximumExpansions, HashSet<ConnectionDirection>? allowedDirections)
    {
        if (maximumExpansions < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumExpansions));
        }

        MaximumExpansions = maximumExpansions;
        AllowedDirections = allowedDirections is null ? [] : [.. allowedDirections];
    }
}