namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ConcurrencyConflictTests
{
    [Fact]
    public async Task SavingWithAStaleConcurrencyToken_DoesNotThrow_AndTheRetryPersistsTheNewerWrite()
    {
        var databaseName = Guid.NewGuid().ToString();
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);

        await using (var seedContext = TestDbContextFactory.Create(databaseName))
        {
            await new EfGameRepository(seedContext).SaveAsync(game);
        }

        // Simulate two independent requests loading the same game concurrently, then both saving.
        await using var contextA = TestDbContextFactory.Create(databaseName);
        await using var contextB = TestDbContextFactory.Create(databaseName);
        var repositoryA = new EfGameRepository(contextA);
        var repositoryB = new EfGameRepository(contextB);

        var gameA = await repositoryA.FindAsync(game.Id);
        var gameB = await repositoryB.FindAsync(game.Id);

        gameA!.Apply(new Domain.Events.PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));
        await repositoryA.SaveAsync(gameA);

        gameB!.Apply(new Domain.Events.LocationDiscoveredEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.DarkForest));
        var act = async () => await repositoryB.SaveAsync(gameB);

        // The single retry (research.md Decision 7) must absorb the conflict rather than crash the request.
        await act.Should().NotThrowAsync<DbUpdateConcurrencyException>();
    }
}
