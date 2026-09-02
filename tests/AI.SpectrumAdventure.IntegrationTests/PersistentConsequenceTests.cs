namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Worlds;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class PersistentConsequenceTests
{
    [Fact]
    public async Task ApplyWorldEvent_PersistsPathNpcAndPuzzleConsequences()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new EfWorldRepository(context);
        var game = await new StartGameUseCase(new EfGameRepository(context), null, repository).ExecuteAsync();
        var world = (await repository.FindAsync(game.WorldId!.Value))!;
        var connection = world.Connections.First();
        var npc = game.Npcs.First();
        var destination = world.GetLocation(connection.DestinationLocationId);
        var apply = new ApplyWorldEventUseCase(repository);

        (await apply.ExecuteAsync(game, new ConnectionStateChangedWorldEvent(WorldEventId.New(), world.Events.Count + 1, game.CreatedAt, "WorldRule:collapse", connection.Id, connection.Visibility, ConnectionAvailability.Blocked))).Success.Should().BeTrue();
        world = (await repository.FindAsync(game.WorldId!.Value))!;
        (await apply.ExecuteAsync(game, new NpcMovedWorldEvent(WorldEventId.New(), world.Events.Count + 1, game.CreatedAt, "TurnElapsed:patrol", npc.Id, destination.Id))).Success.Should().BeTrue();
        world = (await repository.FindAsync(game.WorldId!.Value))!;
        (await apply.ExecuteAsync(game, new PuzzleStateChangedWorldEvent(WorldEventId.New(), world.Events.Count + 1, game.CreatedAt, "PuzzleOutcome:solved", game.Puzzle.Id, true))).Success.Should().BeTrue();
        game.Apply(new NpcRelationshipChangedEvent(Guid.NewGuid(), game.CreatedAt, npc.Id, "npc-trust-earned"));
        await repository.SaveWorldAndGameAsync((await repository.FindAsync(game.WorldId!.Value))!, game);
        context.ChangeTracker.Clear();

        var reloaded = (await repository.FindAsync(game.WorldId!.Value))!;
        reloaded.FindConnection(connection.SourceLocationId, connection.Direction)!.Availability.Should().Be(ConnectionAvailability.Blocked);
        reloaded.NpcLocations[npc.Id].Should().Be(destination.Id);
        reloaded.PuzzleStates[game.Puzzle.Id].Should().BeTrue();
        var reloadedGame = (await new EfGameRepository(context).FindAsync(game.Id))!;
        reloadedGame.GetNpc(npc.Id).RelationshipFlags.Should().ContainSingle(flag => flag.Key == "npc-trust-earned");
    }
}