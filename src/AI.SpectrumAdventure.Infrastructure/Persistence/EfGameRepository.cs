namespace AI.SpectrumAdventure.Infrastructure.Persistence;

using System.Diagnostics;
using System.Text.Json;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using Microsoft.EntityFrameworkCore;

/// <summary>EF Core-backed IGameRepository: serializes Game to a GameSnapshot JSON blob (research.md Decision 1),
/// with a single automatic retry on an optimistic concurrency conflict (research.md Decision 7).</summary>
public sealed class EfGameRepository(AdventureDbContext dbContext) : IGameRepository
{
    private static readonly ActivitySource ActivitySource = new("AI.SpectrumAdventure.Infrastructure");
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    public async Task<Game?> FindAsync(GameId id, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("persistence.game.load");
        activity?.SetTag("persistence.operation", "load");

        try
        {
            var record = await dbContext.Games.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id.Value, cancellationToken);
            if (record is null)
            {
                activity?.SetTag("persistence.found", false);
                return null;
            }

            var snapshot = JsonSerializer.Deserialize<GameSnapshot>(record.Json, SerializerOptions)
                ?? throw new InvalidOperationException($"Corrupt game snapshot for game {id.Value}.");

            activity?.SetTag("persistence.found", true);
            return Game.FromSnapshot(snapshot);
        }
        catch (Exception ex)
        {
            activity?.SetTag("persistence.success", false);
            activity?.SetTag("persistence.error_type", ex.GetType().Name);
            throw;
        }
    }

    public async Task SaveAsync(Game game, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("persistence.game.save");
        activity?.SetTag("persistence.operation", "save");

        try
        {
            var json = JsonSerializer.Serialize(game.ToSnapshot(), SerializerOptions);
            var existing = await dbContext.Games.FirstOrDefaultAsync(g => g.Id == game.Id.Value, cancellationToken);

            if (existing is null)
            {
                dbContext.Games.Add(new GameRecord
                {
                    Id = game.Id.Value,
                    AdventureVersionId = game.AdventureVersionId?.Value,
                    Json = json,
                    CreatedAt = game.CreatedAt,
                    UpdatedAt = game.UpdatedAt,
                    ConcurrencyToken = Guid.NewGuid(),
                });
            }
            else
            {
                existing.Json = json;
                existing.AdventureVersionId = game.AdventureVersionId?.Value;
                existing.UpdatedAt = game.UpdatedAt;
                existing.ConcurrencyToken = Guid.NewGuid();
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                activity?.SetTag("persistence.concurrency_retry", true);
                foreach (var entry in dbContext.ChangeTracker.Entries<GameRecord>())
                {
                    entry.OriginalValues["ConcurrencyToken"] = entry.CurrentValues["ConcurrencyToken"];
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            activity?.SetTag("persistence.success", true);
        }
        catch (Exception ex)
        {
            activity?.SetTag("persistence.success", false);
            activity?.SetTag("persistence.error_type", ex.GetType().Name);
            throw;
        }
    }
}
