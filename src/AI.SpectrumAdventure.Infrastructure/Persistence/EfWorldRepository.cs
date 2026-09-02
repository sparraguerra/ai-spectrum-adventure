namespace AI.SpectrumAdventure.Infrastructure.Persistence;

using System.Diagnostics;
using System.Text.Json;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Lore;
using AI.SpectrumAdventure.Domain.Worlds;
using Microsoft.EntityFrameworkCore;

public sealed class EfWorldRepository(AdventureDbContext dbContext) : IWorldRepository
{
    private static readonly ActivitySource ActivitySource = new("AI.SpectrumAdventure.Infrastructure");
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions GameSnapshotSerializerOptions = new() { WriteIndented = false };

    public async Task CreateInitialWorldAsync(World world, Game game, CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsRelational())
        {
            dbContext.Worlds.Add(ToRecord(world));
            await new EfGameRepository(dbContext).SaveAsync(game, cancellationToken);
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Worlds.Add(ToRecord(world));
        await new EfGameRepository(dbContext).SaveAsync(game, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<World?> FindAsync(WorldId id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Worlds.AsNoTracking()
            .Include(world => world.Regions).Include(world => world.Locations).Include(world => world.Connections)
            .Include(world => world.GenerationMetadata).Include(world => world.Events).Include(world => world.LoreEntries)
            .SingleOrDefaultAsync(world => world.Id == id.Value, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task SaveAsync(World world, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("world.persistence.save");
        var existing = await dbContext.Worlds.Include(record => record.Regions).Include(record => record.Locations)
            .Include(record => record.Connections).Include(record => record.GenerationMetadata).Include(record => record.Events).Include(record => record.LoreEntries)
            .SingleOrDefaultAsync(record => record.Id == world.Id.Value, cancellationToken);
        if (existing is null) dbContext.Worlds.Add(ToRecord(world));
        else AppendNewRecords(existing, world);
        await dbContext.SaveChangesAsync(cancellationToken);
        activity?.SetTag("world.persistence_success", true);
    }

    public async Task SaveWorldAndGameAsync(World world, Game game, CancellationToken cancellationToken = default)
    {
        if (game.WorldId != world.Id) throw new InvalidOperationException("The game must be bound to the world being saved.");

        if (!dbContext.Database.IsRelational())
        {
            await SaveWorldAndGameCoreAsync(world, game, cancellationToken);
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await SaveWorldAndGameCoreAsync(world, game, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<World> PersistExpansionAsync(World world, WorldExpandedEvent expansion, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("world.persistence.expansion");
        var existing = await FindAsync(world.Id, cancellationToken);
        if (existing?.HasGenerationKey(expansion.Metadata.GenerationKey) is true)
        {
            activity?.SetTag("world.duplicate_prevented", true);
            return existing;
        }

        if (!dbContext.Database.IsRelational())
        {
            await SaveAsync(world, cancellationToken);
            return world;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await SaveAsync(world, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            activity?.SetTag("world.persistence_success", true);
            return world;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            var winner = await FindAsync(world.Id, cancellationToken);
            if (winner?.HasGenerationKey(expansion.Metadata.GenerationKey) is true)
            {
                activity?.SetTag("world.duplicate_prevented", true);
                return winner;
            }
            activity?.SetTag("world.persistence_success", false);
            throw;
        }
    }

    public async Task<World> MaterializeLegacyWorldAsync(Game game, CancellationToken cancellationToken = default)
    {
        if (game.WorldId is { } worldId)
        {
            var existing = await FindAsync(worldId, cancellationToken);
            if (existing is not null) return existing;
        }

        var materialized = await dbContext.Worlds.AsNoTracking().SingleOrDefaultAsync(world => world.LegacyGameId == game.Id.Value, cancellationToken);
        if (materialized is not null) return (await FindAsync(new WorldId(materialized.Id), cancellationToken))!;

        var world = WorldBootstrapFactory.Create(game, WorldId.New());
        var record = ToRecord(world);
        record.LegacyGameId = game.Id.Value;
        dbContext.Worlds.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return world;
    }

    private static WorldRecord ToRecord(World world) => new()
    {
        Id = world.Id.Value, Seed = world.Seed.Value, GenerationVersion = world.GenerationVersion.Value, ConcurrencyToken = Guid.NewGuid(),
        Regions = world.Regions.Select(region => new RegionRecord { Id = region.Id.Value, WorldId = world.Id.Value, Type = (int)region.Type, TerrainProfileJson = JsonSerializer.Serialize(region.TerrainProfile, SerializerOptions), MaximumExpansions = region.ExpansionPolicy.MaximumExpansions, AllowedDirectionsJson = JsonSerializer.Serialize(region.ExpansionPolicy.AllowedDirections, SerializerOptions) }).ToList(),
        Locations = world.Locations.Select(location => new WorldLocationRecord { Id = location.Id.Value, WorldId = world.Id.Value, RegionId = location.RegionId.Value, Type = (int)location.Type, StructuralNameKey = location.StructuralNameKey, BaseDescription = location.BaseDescription, EnvironmentJson = JsonSerializer.Serialize(location.Environment, SerializerOptions), State = (int)location.State, SceneVersion = location.SceneVersion }).ToList(),
        Connections = world.Connections.Select(connection => new WorldConnectionRecord { Id = connection.Id.Value, WorldId = world.Id.Value, SourceLocationId = connection.SourceLocationId.Value, DestinationLocationId = connection.DestinationLocationId.Value, Direction = (int)connection.Direction, Visibility = (int)connection.Visibility, Availability = (int)connection.Availability }).ToList(),
        GenerationMetadata = world.GenerationMetadata.Select(metadata => new GenerationMetadataRecord { Id = Guid.NewGuid(), WorldId = world.Id.Value, GenerationKey = metadata.GenerationKey.Value, WorldSeed = metadata.WorldSeed.Value, GenerationVersion = metadata.GenerationVersion.Value, SourceLocationId = metadata.SourceLocationId.Value, Direction = (int)metadata.Direction, ExpansionOrdinal = metadata.ExpansionOrdinal, Reason = metadata.Reason, GeneratedAt = metadata.GeneratedAt }).ToList(),
        Events = world.Events.Select(ToRecord).ToList(),
        LoreEntries = world.LoreEntries.Select(entry => new LoreRecord { Id = Guid.NewGuid(), WorldId = world.Id.Value, Json = JsonSerializer.Serialize(LoreEntrySnapshot.From(entry), SerializerOptions) }).ToList(),
    };

    private static WorldEventRecord ToRecord(WorldEvent worldEvent) => new() { Id = worldEvent.Id.Value, Sequence = worldEvent.Sequence, OccurredAt = worldEvent.OccurredAt, Kind = (int)worldEvent.Kind, Cause = worldEvent.Cause, PayloadJson = JsonSerializer.Serialize(worldEvent, worldEvent.GetType(), SerializerOptions) };

    private void AppendNewRecords(WorldRecord existing, World world)
    {
        var replacement = ToRecord(world);
        existing.Seed = replacement.Seed; existing.GenerationVersion = replacement.GenerationVersion; existing.ConcurrencyToken = Guid.NewGuid();
        dbContext.WorldRegions.AddRange(replacement.Regions.Where(region => existing.Regions.All(current => current.Id != region.Id)));
        dbContext.WorldLocations.AddRange(replacement.Locations.Where(location => existing.Locations.All(current => current.Id != location.Id)));
        dbContext.WorldConnections.AddRange(replacement.Connections.Where(connection => existing.Connections.All(current => current.Id != connection.Id)));
        dbContext.WorldGenerationMetadata.AddRange(replacement.GenerationMetadata.Where(metadata => existing.GenerationMetadata.All(current => current.GenerationKey != metadata.GenerationKey)));
        dbContext.WorldLore.AddRange(replacement.LoreEntries.Where(entry => existing.LoreEntries.All(current => current.Json != entry.Json)));
        var newEvents = replacement.Events.Where(@event => existing.Events.All(current => current.Id != @event.Id)).ToList();
        if (newEvents.Any(@event => existing.Events.Any(current => current.Sequence == @event.Sequence)))
        {
            throw new DbUpdateConcurrencyException("A conflicting world event sequence was persisted by another writer.");
        }
        foreach (var @event in newEvents)
        {
            @event.WorldId = existing.Id;
        }
        dbContext.WorldEvents.AddRange(newEvents);

        foreach (var location in existing.Locations)
        {
            var current = replacement.Locations.SingleOrDefault(value => value.Id == location.Id);
            if (current is not null)
            {
                location.State = current.State;
                location.SceneVersion = current.SceneVersion;
            }
        }

        foreach (var connection in existing.Connections)
        {
            var current = replacement.Connections.SingleOrDefault(value => value.Id == connection.Id);
            if (current is not null)
            {
                connection.Visibility = current.Visibility;
                connection.Availability = current.Availability;
            }
        }
    }

    private async Task SaveWorldAndGameCoreAsync(World world, Game game, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Worlds.Include(record => record.Regions).Include(record => record.Locations)
            .Include(record => record.Connections).Include(record => record.GenerationMetadata).Include(record => record.Events).Include(record => record.LoreEntries)
            .SingleOrDefaultAsync(record => record.Id == world.Id.Value, cancellationToken);
        if (existing is null) dbContext.Worlds.Add(ToRecord(world));
        else AppendNewRecords(existing, world);

        var gameRecord = await dbContext.Games.SingleOrDefaultAsync(record => record.Id == game.Id.Value, cancellationToken);
        var snapshot = JsonSerializer.Serialize(game.ToSnapshot(), GameSnapshotSerializerOptions);
        if (gameRecord is null)
        {
            dbContext.Games.Add(new GameRecord { Id = game.Id.Value, Json = snapshot, CreatedAt = game.CreatedAt, UpdatedAt = game.UpdatedAt, ConcurrencyToken = Guid.NewGuid() });
        }
        else
        {
            gameRecord.Json = snapshot;
            gameRecord.UpdatedAt = game.UpdatedAt;
            gameRecord.ConcurrencyToken = Guid.NewGuid();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static World ToDomain(WorldRecord record)
    {
        var regions = record.Regions.Select(region => new Region(new RegionId(region.Id), (RegionType)region.Type, JsonSerializer.Deserialize<TerrainKind[]>(region.TerrainProfileJson, SerializerOptions)!, new RegionExpansionPolicy(region.MaximumExpansions, JsonSerializer.Deserialize<ConnectionDirection[]>(region.AllowedDirectionsJson, SerializerOptions)!.ToHashSet()), record.Locations.Where(location => location.RegionId == region.Id).Select(location => new LocationId(location.Id)).ToHashSet()));
        var locations = record.Locations.Select(location => new WorldLocation(new LocationId(location.Id), new RegionId(location.RegionId), (LocationType)location.Type, location.StructuralNameKey, location.BaseDescription, JsonSerializer.Deserialize<EnvironmentalProperties>(location.EnvironmentJson, SerializerOptions)!, (LocationState)location.State, location.SceneVersion));
        var connections = record.Connections.Select(connection => new WorldConnection(new ConnectionId(connection.Id), new LocationId(connection.SourceLocationId), new LocationId(connection.DestinationLocationId), (ConnectionDirection)connection.Direction, (ConnectionVisibility)connection.Visibility, (ConnectionAvailability)connection.Availability));
        var metadata = record.GenerationMetadata.Select(value => new GeneratedContentMetadata(new GenerationKey(value.GenerationKey), new WorldSeed(value.WorldSeed), new GenerationVersion(value.GenerationVersion), new LocationId(value.SourceLocationId), (ConnectionDirection)value.Direction, value.ExpansionOrdinal, value.Reason, value.GeneratedAt));
        var events = record.Events.OrderBy(@event => @event.Sequence).Select(ToDomain);
        var loreEntries = record.LoreEntries.Select(entry => (JsonSerializer.Deserialize<LoreEntrySnapshot>(entry.Json, SerializerOptions) ?? throw new InvalidOperationException("Persisted lore is invalid.")).ToDomain());
        return new World(new WorldId(record.Id), new WorldSeed(record.Seed), new GenerationVersion(record.GenerationVersion), regions, locations, connections, metadata, events, loreEntries);
    }

    private sealed record LoreEntrySnapshot(string Id, LoreCategory Category, LoreScope Scope, LoreTruthClassification TruthClassification, string ContentKey, string Content, string[] SubjectReferences)
    {
        public static LoreEntrySnapshot From(LoreEntry entry) => new(entry.Id.Value, entry.Category, entry.Scope, entry.TruthClassification, entry.ContentKey, entry.Content, [.. entry.SubjectReferences]);
        public LoreEntry ToDomain() => new(new LoreId(Id), Category, Scope, TruthClassification, ContentKey, Content, SubjectReferences);
    }

    private static WorldEvent ToDomain(WorldEventRecord record) => (WorldEventKind)record.Kind switch
    {
        WorldEventKind.Expansion => (WorldEvent?)JsonSerializer.Deserialize<WorldExpandedEvent>(record.PayloadJson, SerializerOptions),
        WorldEventKind.ConnectionChanged => (WorldEvent?)JsonSerializer.Deserialize<ConnectionStateChangedWorldEvent>(record.PayloadJson, SerializerOptions),
        WorldEventKind.EnvironmentChanged => (WorldEvent?)JsonSerializer.Deserialize<LocationStateChangedWorldEvent>(record.PayloadJson, SerializerOptions),
        WorldEventKind.ObjectChanged => (WorldEvent?)JsonSerializer.Deserialize<ObjectStateChangedWorldEvent>(record.PayloadJson, SerializerOptions),
        WorldEventKind.NpcMoved => (WorldEvent?)JsonSerializer.Deserialize<NpcMovedWorldEvent>(record.PayloadJson, SerializerOptions),
        WorldEventKind.PuzzleConsequence => (WorldEvent?)JsonSerializer.Deserialize<PuzzleStateChangedWorldEvent>(record.PayloadJson, SerializerOptions),
        _ => throw new InvalidOperationException($"Unsupported persisted world event kind '{record.Kind}'.")
    } ?? throw new InvalidOperationException($"Persisted world event '{record.Id}' has no valid payload.");
}