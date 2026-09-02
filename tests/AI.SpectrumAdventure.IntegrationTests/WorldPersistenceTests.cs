namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class WorldPersistenceTests
{
    [Fact]
    public async Task StartGame_PersistsInitialWorldBeforeItsBoundGame()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var context = TestDbContextFactory.Create(databaseName);
        var game = await new StartGameUseCase(new EfGameRepository(context), null, new EfWorldRepository(context)).ExecuteAsync();

        game.WorldId.Should().NotBeNull();
        context.Worlds.Should().ContainSingle(world => world.Id == game.WorldId!.Value.Value);
        context.Games.Should().ContainSingle(record => record.Id == game.Id.Value);
    }

    [Fact]
    public async Task MaterializeLegacyWorldAsync_IsIdempotentAndDoesNotChangeRecordedProgress()
    {
        await using var context = TestDbContextFactory.Create();
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        game.Apply(new LocationDiscoveredEvent(Guid.NewGuid(), game.CreatedAt, AdventureWorldFactory.ForestEntrance));
        var repository = new EfWorldRepository(context);

        var first = await repository.MaterializeLegacyWorldAsync(game);
        var second = await repository.MaterializeLegacyWorldAsync(game);

        second.Id.Should().Be(first.Id);
        context.Worlds.Should().ContainSingle();
        game.CurrentLocation.Id.Should().Be(AdventureWorldFactory.ForestEntrance);
        game.EventHistory.Should().ContainSingle();
    }

    [Fact]
    public async Task SaveAsync_ReloadsChangedConnectionAndItsAuthoritativeEvent()
    {
        await using var context = TestDbContextFactory.Create();
        var game = await new StartGameUseCase(new EfGameRepository(context), null, new EfWorldRepository(context)).ExecuteAsync();
        var repository = new EfWorldRepository(context);
        var world = (await repository.FindAsync(game.WorldId!.Value))!;
        var connection = world.Connections.First();
        var change = new ConnectionStateChangedWorldEvent(
            WorldEventId.New(), world.Events.Count + 1, DateTimeOffset.UtcNow, WorldEvent.FormatCause(WorldEventCause.WorldRule, "collapsed-bridge"),
            connection.Id, ConnectionVisibility.Discovered, ConnectionAvailability.Blocked);

        world.Apply(change);
        await repository.SaveAsync(world);
        context.ChangeTracker.Clear();

        var reloaded = (await repository.FindAsync(world.Id))!;
        reloaded.FindConnection(connection.SourceLocationId, connection.Direction)!.Availability.Should().Be(ConnectionAvailability.Blocked);
        reloaded.Events.Should().ContainSingle(worldEvent => worldEvent.Kind == WorldEventKind.ConnectionChanged);
    }
}