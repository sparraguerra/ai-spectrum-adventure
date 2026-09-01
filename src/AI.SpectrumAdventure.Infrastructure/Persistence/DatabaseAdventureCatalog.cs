namespace AI.SpectrumAdventure.Infrastructure.Persistence;

using System.Text.Json;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Games;
using Microsoft.EntityFrameworkCore;

public sealed class DatabaseAdventureCatalog(AdventureDbContext? dbContext, string localAdventureDirectory) : IAdventureCatalog
{
    public async Task<IReadOnlyCollection<AdventureCatalogItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (dbContext is null)
        {
            return [.. await ReadLocalSummariesAsync(cancellationToken)];
        }

        await SeedFromLocalFilesWhenEmptyAsync(cancellationToken);

        return await dbContext.Adventures
            .AsNoTracking()
            .OrderBy(adventure => adventure.Title)
            .Select(adventure => new AdventureCatalogItem(adventure.Id, adventure.Title))
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GetDefinitionJsonAsync(string adventureId, CancellationToken cancellationToken = default)
    {
        if (dbContext is not null)
        {
            await SeedFromLocalFilesWhenEmptyAsync(cancellationToken);

            var json = await dbContext.Adventures
                .AsNoTracking()
                .Where(adventure => adventure.Id == adventureId)
                .Select(adventure => adventure.Json)
                .FirstOrDefaultAsync(cancellationToken);

            if (json is not null)
            {
                return json;
            }
        }

        var path = Path.Combine(localAdventureDirectory, $"{adventureId}.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Adventure definition '{adventureId}' was not found in the database or packaged content.", path);
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    private async Task SeedFromLocalFilesWhenEmptyAsync(CancellationToken cancellationToken)
    {
        if (dbContext is null || await dbContext.Adventures.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var file in EnumerateLocalAdventureFiles())
        {
            var json = await File.ReadAllTextAsync(file, cancellationToken);
            var summary = ReadSummary(json);
            dbContext.Adventures.Add(new AdventureRecord
            {
                Id = summary.Id,
                Title = summary.Title,
                Json = json,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<AdventureCatalogItem>> ReadLocalSummariesAsync(CancellationToken cancellationToken)
    {
        var items = new List<AdventureCatalogItem>();
        foreach (var file in EnumerateLocalAdventureFiles())
        {
            items.Add(ReadSummary(await File.ReadAllTextAsync(file, cancellationToken)));
        }

        return [.. items.OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)];
    }

    private IEnumerable<string> EnumerateLocalAdventureFiles() =>
        Directory.Exists(localAdventureDirectory)
            ? Directory.EnumerateFiles(localAdventureDirectory, "*.json", SearchOption.TopDirectoryOnly)
            : [];

    private static AdventureCatalogItem ReadSummary(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var id = root.GetProperty("id").GetString() ?? throw new InvalidOperationException("Adventure JSON must define id.");
        var title = root.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : id;

        return new AdventureCatalogItem(id, string.IsNullOrWhiteSpace(title) ? id : title!);
    }
}