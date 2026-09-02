namespace AI.SpectrumAdventure.IntegrationTests;

using System.Data.Common;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Builds an isolated in-memory AdventureDbContext per test (research.md Decision 6: no live DB in CI).</summary>
public static class TestDbContextFactory
{
    public const string PostgreSqlConnectionStringEnvironmentVariable = "AI_SPECTRUM_ADVENTURE_TEST_POSTGRES_CONNECTION_STRING";

    public static AdventureDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AdventureDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new AdventureDbContext(options);
    }

    public static AdventureDbContext CreatePostgreSql(string connectionString, string? schema = null)
    {
        if (schema is not null)
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            builder["Search Path"] = schema;
            connectionString = builder.ConnectionString;
        }

        var options = new DbContextOptionsBuilder<AdventureDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AdventureDbContext(options);
    }
}
