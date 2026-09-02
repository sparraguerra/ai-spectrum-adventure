namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class ProgressiveMapDiscoveryTests
{
    [Fact]
    public async Task ConfirmedPath_UpdatesTheReloadedMapWithItsBlockedState()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new EfWorldRepository(context);
        var game = await new StartGameUseCase(new EfGameRepository(context), null, repository).ExecuteAsync();
        var world = (await repository.FindAsync(game.WorldId!.Value))!;
        var connection = world.Connections.First();
        game.PlayerKnowledge.DiscoverLocation(connection.SourceLocationId, game.CreatedAt);
        game.PlayerKnowledge.DiscoverLocation(connection.DestinationLocationId, game.CreatedAt);
        game.PlayerKnowledge.DiscoverConnection(connection.Id, game.CreatedAt);
        await new ApplyWorldEventUseCase(repository).ExecuteAsync(game, new ConnectionStateChangedWorldEvent(WorldEventId.New(), world.Events.Count + 1, game.CreatedAt, "WorldRule:collapse", connection.Id, connection.Visibility, ConnectionAvailability.Blocked));
        context.ChangeTracker.Clear();

        var reloadedGame = (await new EfGameRepository(context).FindAsync(game.Id))!;
        var reloadedWorld = (await repository.FindAsync(game.WorldId!.Value))!;
        var map = GetDiscoveredMapQuery.Execute(reloadedGame, reloadedWorld);

        map.Connections.Should().ContainSingle(path => path.Id == connection.Id.Value && path.Availability == ConnectionAvailability.Blocked);
    }
}