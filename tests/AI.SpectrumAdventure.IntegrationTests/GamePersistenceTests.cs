namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

public class GamePersistenceTests
{
    [Fact]
    public async Task SaveThenFind_ReproducesAnIdenticalGameState()
    {
        var databaseName = Guid.NewGuid().ToString();
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));
        game.Apply(new ItemTakenEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.Sign, AdventureWorldFactory.ForestEntrance));

        await using (var writeContext = TestDbContextFactory.Create(databaseName))
        {
            var repository = new EfGameRepository(writeContext);
            await repository.SaveAsync(game);
        }

        await using var readContext = TestDbContextFactory.Create(databaseName);
        var reloaded = await new EfGameRepository(readContext).FindAsync(game.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Player.CurrentLocationId.Should().Be(game.Player.CurrentLocationId);
        reloaded.Player.Inventory.Contains(AdventureWorldFactory.Sign).Should().BeTrue();
        reloaded.EventHistory.Should().HaveCount(game.EventHistory.Count);
    }

    [Fact]
    public async Task FindAsync_UnknownGameId_ReturnsNull()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new EfGameRepository(context);

        var result = await repository.FindAsync(GameId.New());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ContinuingAGame_AfterReload_CanStillProcessNewActions()
    {
        var databaseName = Guid.NewGuid().ToString();
        var original = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);

        await using (var writeContext = TestDbContextFactory.Create(databaseName))
        {
            await new EfGameRepository(writeContext).SaveAsync(original);
        }

        await using var readContext = TestDbContextFactory.Create(databaseName);
        var reloaded = await new EfGameRepository(readContext).FindAsync(original.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));
        reloaded.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.DarkForest);
    }
}
