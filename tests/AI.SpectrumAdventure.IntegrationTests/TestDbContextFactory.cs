namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Builds an isolated in-memory AdventureDbContext per test (research.md Decision 6: no live DB in CI).</summary>
public static class TestDbContextFactory
{
    public static AdventureDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AdventureDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new AdventureDbContext(options);
    }
}
