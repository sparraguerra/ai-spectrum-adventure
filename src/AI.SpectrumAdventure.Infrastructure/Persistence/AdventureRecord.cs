namespace AI.SpectrumAdventure.Infrastructure.Persistence;

public sealed class AdventureRecord
{
    public required string Id { get; set; }

    public required string Title { get; set; }

    public required string Json { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}