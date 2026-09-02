namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class WorldGenerationConcurrencyTests
{
    [RequiresPostgreSqlFact]
    public async Task PersistExpansionAsync_RepeatedGenerationKeyReturnsSinglePersistedExpansion()
    {
        var connectionString = Environment.GetEnvironmentVariable(TestDbContextFactory.PostgreSqlConnectionStringEnvironmentVariable)!;
        var schema = $"world_concurrency_{Guid.NewGuid():N}";
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var worldId = WorldId.New();
        var candidateOne = WorldBootstrapFactory.Create(game, worldId);
        var candidateTwo = WorldBootstrapFactory.Create(game, worldId);
        var expansion = CreateExpansion(candidateOne);
        candidateOne.Apply(expansion);
        candidateTwo.Apply(CreateExpansion(candidateTwo));

        await using var adminContext = TestDbContextFactory.CreatePostgreSql(connectionString);
    await ExecuteSchemaCommandAsync(adminContext, "CREATE SCHEMA " + schema);
        try
        {
            await using (var initialContext = TestDbContextFactory.CreatePostgreSql(connectionString, schema))
            {
                await initialContext.Database.EnsureCreatedAsync();
                await new EfWorldRepository(initialContext).CreateInitialWorldAsync(WorldBootstrapFactory.Create(game, worldId), game);
            }

            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var firstWriter = PersistAsync(candidateOne, expansion, connectionString, schema, start.Task);
            var secondWriter = PersistAsync(candidateTwo, CreateExpansion(candidateTwo), connectionString, schema, start.Task);
            start.SetResult();

            var resolved = await Task.WhenAll(firstWriter, secondWriter);

            resolved.Should().OnlyContain(world => world.GenerationMetadata.Count(metadata => metadata.GenerationKey.Value == "forest-up-1") == 1);
            await using var verificationContext = TestDbContextFactory.CreatePostgreSql(connectionString, schema);
            (await verificationContext.WorldGenerationMetadata.CountAsync(metadata => metadata.GenerationKey == "forest-up-1")).Should().Be(1);
        }
        finally
        {
            await ExecuteSchemaCommandAsync(adminContext, "DROP SCHEMA IF EXISTS " + schema + " CASCADE");
        }
    }

    private static async Task<World> PersistAsync(World world, WorldExpandedEvent expansion, string connectionString, string schema, Task start)
    {
        await start;
        await using var context = TestDbContextFactory.CreatePostgreSql(connectionString, schema);
        return await new EfWorldRepository(context).PersistExpansionAsync(world, expansion);
    }

    private static async Task ExecuteSchemaCommandAsync(AdventureDbContext context, string commandText)
    {
        await context.Database.OpenConnectionAsync();
        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static WorldExpandedEvent CreateExpansion(World world)
    {
        var region = world.Regions.Single();
        var location = new WorldLocation(new LocationId("forest-canopy"), region.Id, LocationType.Path, "forest-canopy", "A high forest path.", new EnvironmentalProperties(TerrainKind.Forest, "temperate", "sunlit"));
        var connection = new WorldConnection(new ConnectionId("forest-entrance:up"), AdventureWorldFactory.ForestEntrance, location.Id, ConnectionDirection.Up, ConnectionVisibility.Discovered);
        var metadata = new GeneratedContentMetadata(new GenerationKey("forest-up-1"), world.Seed, world.GenerationVersion, AdventureWorldFactory.ForestEntrance, ConnectionDirection.Up, 1, "test", DateTimeOffset.UtcNow);
        return new WorldExpandedEvent(WorldEventId.New(), world.Events.Count + 1, DateTimeOffset.UtcNow, "test", null, location, connection, metadata);
    }
}

public sealed class RequiresPostgreSqlFactAttribute : FactAttribute
{
    public RequiresPostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(TestDbContextFactory.PostgreSqlConnectionStringEnvironmentVariable)))
        {
            Skip = $"Set {TestDbContextFactory.PostgreSqlConnectionStringEnvironmentVariable} to run PostgreSQL concurrency tests.";
        }
    }
}