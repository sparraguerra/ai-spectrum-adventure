namespace AI.SpectrumAdventure.Infrastructure.Persistence;

public sealed class WorldRecord
{
    public Guid Id { get; set; }
    public Guid? LegacyGameId { get; set; }
    public required string Seed { get; set; }
    public required string GenerationVersion { get; set; }
    public Guid ConcurrencyToken { get; set; }
    public List<RegionRecord> Regions { get; set; } = [];
    public List<WorldLocationRecord> Locations { get; set; } = [];
    public List<WorldConnectionRecord> Connections { get; set; } = [];
    public List<GenerationMetadataRecord> GenerationMetadata { get; set; } = [];
    public List<WorldEventRecord> Events { get; set; } = [];
    public List<LoreRecord> LoreEntries { get; set; } = [];
}

public sealed class RegionRecord
{
    public required string Id { get; set; }
    public Guid WorldId { get; set; }
    public int Type { get; set; }
    public required string TerrainProfileJson { get; set; }
    public int MaximumExpansions { get; set; }
    public required string AllowedDirectionsJson { get; set; }
}

public sealed class WorldLocationRecord
{
    public required string Id { get; set; }
    public Guid WorldId { get; set; }
    public required string RegionId { get; set; }
    public int Type { get; set; }
    public required string StructuralNameKey { get; set; }
    public required string BaseDescription { get; set; }
    public required string EnvironmentJson { get; set; }
    public int State { get; set; }
    public int SceneVersion { get; set; }
}

public sealed class WorldPresentationRecord
{
    public Guid WorldId { get; set; }
    public required string LocationId { get; set; }
    public int SceneVersion { get; set; }
    public required string FactualName { get; set; }
    public required string FactualDescription { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Atmosphere { get; set; }
    public required string VisualCharacteristicsJson { get; set; }
}

public sealed class WorldConnectionRecord
{
    public required string Id { get; set; }
    public Guid WorldId { get; set; }
    public required string SourceLocationId { get; set; }
    public required string DestinationLocationId { get; set; }
    public int Direction { get; set; }
    public int Visibility { get; set; }
    public int Availability { get; set; }
}

public sealed class GenerationMetadataRecord
{
    public Guid Id { get; set; }
    public Guid WorldId { get; set; }
    public required string GenerationKey { get; set; }
    public required string WorldSeed { get; set; }
    public required string GenerationVersion { get; set; }
    public required string SourceLocationId { get; set; }
    public int Direction { get; set; }
    public int ExpansionOrdinal { get; set; }
    public required string Reason { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
}

public sealed class WorldEventRecord
{
    public Guid Id { get; set; }
    public Guid WorldId { get; set; }
    public long Sequence { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public int Kind { get; set; }
    public required string Cause { get; set; }
    public required string PayloadJson { get; set; }
}

// These sets establish the World-owned persistence surface. Their detailed models arrive with later feature tasks.
public sealed class LoreRecord { public Guid Id { get; set; } public Guid WorldId { get; set; } public required string Json { get; set; } }
public sealed class WorldNpcStateRecord { public Guid Id { get; set; } public Guid WorldId { get; set; } public required string Json { get; set; } }
public sealed class WorldPuzzleRecord { public Guid Id { get; set; } public Guid WorldId { get; set; } public required string Json { get; set; } }