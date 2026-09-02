namespace AI.SpectrumAdventure.Domain.Worlds;

using AI.SpectrumAdventure.Domain.Common;

public readonly record struct WorldSeed
{
    public string Value { get; }

    public WorldSeed(string value)
    {
        Value = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A world seed is required.", nameof(value)) : value;
    }

    public override string ToString() => Value;
}

public readonly record struct GenerationVersion
{
    public string Value { get; }

    public GenerationVersion(string value)
    {
        Value = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A generation version is required.", nameof(value)) : value;
    }

    public override string ToString() => Value;
}

public readonly record struct GenerationKey
{
    public string Value { get; }

    public GenerationKey(string value)
    {
        Value = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A generation key is required.", nameof(value)) : value;
    }

    public override string ToString() => Value;
}

public sealed record GeneratedContentMetadata
{
    public GenerationKey GenerationKey { get; }
    public WorldSeed WorldSeed { get; }
    public GenerationVersion GenerationVersion { get; }
    public LocationId SourceLocationId { get; }
    public ConnectionDirection Direction { get; }
    public int ExpansionOrdinal { get; }
    public string Reason { get; }
    public DateTimeOffset GeneratedAt { get; }

    public GeneratedContentMetadata(GenerationKey generationKey, WorldSeed worldSeed, GenerationVersion generationVersion, LocationId sourceLocationId, ConnectionDirection direction, int expansionOrdinal, string reason, DateTimeOffset generatedAt)
    {
        if (expansionOrdinal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expansionOrdinal));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A generation reason is required.", nameof(reason));
        }

        GenerationKey = generationKey;
        WorldSeed = worldSeed;
        GenerationVersion = generationVersion;
        SourceLocationId = sourceLocationId;
        Direction = direction;
        ExpansionOrdinal = expansionOrdinal;
        Reason = reason;
        GeneratedAt = generatedAt;
    }
}