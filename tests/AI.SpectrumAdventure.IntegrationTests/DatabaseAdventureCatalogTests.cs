namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class DatabaseAdventureCatalogTests : IDisposable
{
    private readonly string adventureDirectory = Path.Combine(Path.GetTempPath(), $"spectrum-adventures-{Guid.NewGuid():N}");

    public DatabaseAdventureCatalogTests()
    {
        Directory.CreateDirectory(adventureDirectory);
    }

    [Fact]
    public async Task ListAsync_WhenDatabaseIsUnavailable_ReadsPackagedAdventureJsonFiles()
    {
        await WriteAdventureAsync("crystal-mine", "Crystal Mine");
        var catalog = new DatabaseAdventureCatalog(null, adventureDirectory);

        var adventures = await catalog.ListAsync();

        adventures.Should().ContainSingle(item => item.Id == "crystal-mine" && item.Title == "Crystal Mine");
    }

    [Fact]
    public async Task ListAsync_WhenDatabaseIsEmpty_SeedsPackagedAdventureJsonFilesIntoDatabase()
    {
        await WriteAdventureAsync("crystal-mine", "Crystal Mine");
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
        var catalog = new DatabaseAdventureCatalog(dbContext, adventureDirectory);

        var adventures = await catalog.ListAsync();

        adventures.Should().ContainSingle(item => item.Id == "crystal-mine" && item.Title == "Crystal Mine");
        dbContext.Adventures.Should().ContainSingle(record => record.Id == "crystal-mine" && record.Title == "Crystal Mine");
    }

    [Fact]
    public async Task GetDefinitionJsonAsync_PrefersDatabaseDefinitionOverPackagedJson()
    {
        await WriteAdventureAsync("crystal-mine", "Packaged Crystal Mine");
        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.Adventures.Add(new AdventureRecord
        {
            Id = "crystal-mine",
            Title = "Database Crystal Mine",
            Json = """
            {
              "id": "crystal-mine",
              "title": "Database Crystal Mine"
            }
            """,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();
        var catalog = new DatabaseAdventureCatalog(dbContext, adventureDirectory);

        var json = await catalog.GetDefinitionJsonAsync("crystal-mine");

        json.Should().Contain("Database Crystal Mine");
        json.Should().NotContain("Packaged Crystal Mine");
    }

    private Task WriteAdventureAsync(string id, string title) =>
        File.WriteAllTextAsync(Path.Combine(adventureDirectory, $"{id}.json"), $$"""
        {
          "id": "{{id}}",
          "title": "{{title}}"
        }
        """);

    private static AdventureDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AdventureDbContext>()
            .UseInMemoryDatabase($"adventures-{Guid.NewGuid():N}")
            .Options);

    public void Dispose()
    {
        if (Directory.Exists(adventureDirectory))
        {
            Directory.Delete(adventureDirectory, recursive: true);
        }
    }
}