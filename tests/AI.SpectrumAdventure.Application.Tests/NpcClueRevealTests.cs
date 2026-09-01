namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

/// <summary>A fake INpcAgent test double used to test NpcConversationOrchestrator without any real agent wiring.</summary>
public sealed class FakeNpcAgent(NpcResponse response) : INpcAgent
{
    public Task<NpcResponse> ReplyAsync(NpcContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(response);
}

public class NpcClueRevealTests
{
    private static Game CreateGameAtDarkForest()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));
        return game;
    }

    [Fact]
    public async Task ConverseAsync_NpcRevealsTowerClue_GrantsPlayerTheClue_AndPersistsIt()
    {
        var game = CreateGameAtDarkForest();
        var npcAgent = new FakeNpcAgent(new NpcResponse(
            "hermit", "Beware the door that only opens for those who ask kindly.", ["tower-clue"], null));

        var result = await NpcConversationOrchestrator.ConverseAsync(game, AdventureWorldFactory.Hermit, "What do you know of the tower?", npcAgent);

        result.Success.Should().BeTrue();
        game.Player.KnownClues.Should().Contain(AdventureWorldFactory.TowerClueKey);
    }

    [Fact]
    public async Task ConverseAsync_GenericReplyWithNoReveal_StillRecordsConversationTurn_WithoutGrantingTheClue()
    {
        var game = CreateGameAtDarkForest();
        var npcAgent = new FakeNpcAgent(new NpcResponse("hermit", "Who goes there?", [], null));

        await NpcConversationOrchestrator.ConverseAsync(game, AdventureWorldFactory.Hermit, "Hello?", npcAgent);

        game.Player.KnownClues.Should().BeEmpty();
        game.GetNpc(AdventureWorldFactory.Hermit).ConversationMemory.Should().ContainSingle();
    }

    [Fact]
    public async Task ConverseAsync_NpcNotPresentAtCurrentLocation_FailsWithTargetNotPresent()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var npcAgent = new FakeNpcAgent(new NpcResponse("hermit", "...", [], null));

        var result = await NpcConversationOrchestrator.ConverseAsync(game, AdventureWorldFactory.Hermit, "Hello?", npcAgent);

        result.Success.Should().BeFalse();
        result.Reason.Should().Be("TargetNotPresent");
    }
}
