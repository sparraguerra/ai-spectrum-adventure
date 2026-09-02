namespace AI.SpectrumAdventure.Infrastructure.Persistence;

using System.Text.Json;
using AI.SpectrumAdventure.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

public sealed class WorldEnrichmentRepository(AdventureDbContext dbContext) : IWorldEnrichmentRepository
{
    public async Task<WorldPresentationMetadata?> FindAsync(Guid worldId, string locationId, int sceneVersion, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.WorldPresentations.AsNoTracking()
            .SingleOrDefaultAsync(value => value.WorldId == worldId && value.LocationId == locationId && value.SceneVersion == sceneVersion, cancellationToken);
        return record is null ? null : ToMetadata(record);
    }

    public async Task SaveAsync(WorldPresentationMetadata metadata, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.WorldPresentations
            .SingleOrDefaultAsync(value => value.WorldId == metadata.WorldId && value.LocationId == metadata.LocationId && value.SceneVersion == metadata.SceneVersion, cancellationToken);
        if (record is null)
        {
            dbContext.WorldPresentations.Add(ToRecord(metadata));
        }
        else
        {
            record.FactualName = metadata.FactualName;
            record.FactualDescription = metadata.FactualDescription;
            record.DisplayName = metadata.DisplayName;
            record.Description = metadata.Description;
            record.Atmosphere = metadata.Atmosphere;
            record.VisualCharacteristicsJson = JsonSerializer.Serialize(metadata.VisualCharacteristics);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static WorldPresentationMetadata ToMetadata(WorldPresentationRecord record) =>
        new(record.WorldId, record.LocationId, record.SceneVersion, record.FactualName, record.FactualDescription,
            record.DisplayName, record.Description, record.Atmosphere,
            JsonSerializer.Deserialize<string[]>(record.VisualCharacteristicsJson) ?? []);

    private static WorldPresentationRecord ToRecord(WorldPresentationMetadata metadata) => new()
    {
        WorldId = metadata.WorldId,
        LocationId = metadata.LocationId,
        SceneVersion = metadata.SceneVersion,
        FactualName = metadata.FactualName,
        FactualDescription = metadata.FactualDescription,
        DisplayName = metadata.DisplayName,
        Description = metadata.Description,
        Atmosphere = metadata.Atmosphere,
        VisualCharacteristicsJson = JsonSerializer.Serialize(metadata.VisualCharacteristics),
    };
}