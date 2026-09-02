namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

public sealed class PersistentNpcFlowTests
{
    [Fact]
    public async Task ConversationsAndValidatedRelocationPersistAcrossReload()
    {
        await using var context = TestDbContextFactory.Create();
        var games = new EfGameRepository(context);
        var worlds = new EfWorldRepository(context);
        var game = await new StartGameUseCase(games, null, worlds).ExecuteAsync();
        game.Apply(new AI.SpectrumAdventure.Domain.Events.PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UnixEpoch, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));
        var agent = new FixedNpcAgent();
        await NpcConversationOrchestrator.ConverseAsync(game, AdventureWorldFactory.Hermit, "Hello", agent);
        await NpcConversationOrchestrator.ConverseAsync(game, AdventureWorldFactory.Hermit, "Tell me of the tower", agent);
        var world = (await worlds.FindAsync(game.WorldId!.Value))!;

        NpcWorldStateService.Place(world, game.GetNpc(AdventureWorldFactory.Hermit), AdventureWorldFactory.ForestEntrance, WorldEvent.FormatCause(WorldEventCause.PlayerAction, "returns-to-path"), DateTimeOffset.UnixEpoch.AddMinutes(1));
        await worlds.SaveWorldAndGameAsync(world, game);
        context.ChangeTracker.Clear();

        var reloadedGame = (await games.FindAsync(game.Id))!;
        var reloadedWorld = (await worlds.FindAsync(world.Id))!;
        reloadedGame.GetNpc(AdventureWorldFactory.Hermit).ConversationMemory.Should().HaveCount(2);
        reloadedGame.GetNpc(AdventureWorldFactory.Hermit).WorldLocationId.Should().Be(AdventureWorldFactory.ForestEntrance);
        NpcWorldStateService.GetLocation(reloadedWorld, AdventureWorldFactory.Hermit).Should().Be(AdventureWorldFactory.ForestEntrance);
    }

    private sealed class FixedNpcAgent : AI.SpectrumAdventure.Application.Abstractions.INpcAgent
    {
        public Task<NpcResponse> ReplyAsync(NpcContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new NpcResponse(context.NpcName, "I remember the path.", [], null));
    }
}