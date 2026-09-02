namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Worlds;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class GeneratedLocationConsistencyTests
{
    [Fact]
    public async Task ChangedLocation_SurvivesReturnAndApplicationRestart()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var setup = TestDbContextFactory.Create(databaseName))
        {
            var game = await new StartGameUseCase(new EfGameRepository(setup), null, new EfWorldRepository(setup)).ExecuteAsync();
            var repository = new EfWorldRepository(setup);
            var world = (await repository.FindAsync(game.WorldId!.Value))!;
            var connection = world.Connections.Single(connection => connection.SourceLocationId == game.Player.CurrentLocationId);
            var location = world.GetLocation(connection.DestinationLocationId);
            world.Apply(new LocationStateChangedWorldEvent(WorldEventId.New(), world.Events.Count + 1, DateTimeOffset.UnixEpoch, WorldEvent.FormatCause(WorldEventCause.WorldRule, "storm"), location.Id, LocationState.Altered));
            world.Apply(new ConnectionStateChangedWorldEvent(WorldEventId.New(), world.Events.Count + 1, DateTimeOffset.UnixEpoch, WorldEvent.FormatCause(WorldEventCause.PlayerAction, "charted"), connection.Id, ConnectionVisibility.Discovered, ConnectionAvailability.Available));
            await repository.SaveWorldAndGameAsync(world, game);
        }

        await using var restarted = TestDbContextFactory.Create(databaseName);
        var gameAfterRestart = (await new EfGameRepository(restarted).FindAsync(new GameId((await restarted.Games.SingleAsync()).Id)))!;
        var persistedWorld = (await new EfWorldRepository(restarted).FindAsync(gameAfterRestart.WorldId!.Value))!;
        var changedLocation = persistedWorld.Locations.Single(location => location.State == LocationState.Altered);
        var travel = new TravelToKnownLocationUseCase(new EfWorldRepository(restarted));
        var result = await travel.ExecuteAsync(gameAfterRestart, persistedWorld.Connections.Single(connection => connection.SourceLocationId == gameAfterRestart.Player.CurrentLocationId).Direction.ToString());

        result.Success.Should().BeTrue();
        gameAfterRestart.Player.CurrentLocationId.Should().Be(changedLocation.Id);
    }

    [Fact]
    public async Task ConcurrentWorldSaves_InMemoryProviderPersistsOneCompleteWorldState()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var setup = TestDbContextFactory.Create(databaseName);
        var game = await new StartGameUseCase(new EfGameRepository(setup), null, new EfWorldRepository(setup)).ExecuteAsync();
        var worldId = game.WorldId!.Value;
        await using var firstContext = TestDbContextFactory.Create(databaseName);
        await using var secondContext = TestDbContextFactory.Create(databaseName);
        var firstWorld = (await new EfWorldRepository(firstContext).FindAsync(worldId))!;
        var secondWorld = (await new EfWorldRepository(secondContext).FindAsync(worldId))!;
        var locationId = firstWorld.Locations.First().Id;
        firstWorld.Apply(new LocationStateChangedWorldEvent(WorldEventId.New(), firstWorld.Events.Count + 1, DateTimeOffset.UnixEpoch, WorldEvent.FormatCause(WorldEventCause.WorldRule, "first"), locationId, LocationState.Altered));
        secondWorld.Apply(new LocationStateChangedWorldEvent(WorldEventId.New(), secondWorld.Events.Count + 1, DateTimeOffset.UnixEpoch, WorldEvent.FormatCause(WorldEventCause.WorldRule, "second"), locationId, LocationState.Destroyed));

        await new EfWorldRepository(firstContext).SaveAsync(firstWorld);
        Func<Task> saveStale = () => new EfWorldRepository(secondContext).SaveAsync(secondWorld);

        await saveStale.Should().ThrowAsync<DbUpdateConcurrencyException>();
        await using var verifyContext = TestDbContextFactory.Create(databaseName);
        var persisted = (await new EfWorldRepository(verifyContext).FindAsync(worldId))!;
        persisted.GetLocation(locationId).State.Should().Be(LocationState.Altered);
        persisted.Events.Should().ContainSingle(worldEvent => worldEvent.Cause == WorldEvent.FormatCause(WorldEventCause.WorldRule, "first"));
    }
}