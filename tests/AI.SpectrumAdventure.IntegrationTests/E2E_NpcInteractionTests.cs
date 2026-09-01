namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;

public sealed class E2E_NpcInteractionTests
{
    [Fact]
    public async Task Conversation_RemainsBoundedAcrossMultipleTurns()
    {
        var game = E2ETestSupport.CreateStartedGame();
        AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Go, "north"));
        var npc = new ScriptedNpcAgent(
        [
            new NpcResponse("hermit", "The woods remember patient travelers.", [], ["Ask about the tower."]),
            new NpcResponse("hermit", "The tower answers only a patient hand.", [AdventureWorldFactory.TowerClueKey], null),
            new NpcResponse("hermit", "I know nothing beyond these trees.", ["outside-knowledge"], null),
        ]);

        var first = await NpcConversationOrchestrator.ConverseAsync(game, AdventureWorldFactory.Hermit, "Hello", npc);
        var second = await NpcConversationOrchestrator.ConverseAsync(game, AdventureWorldFactory.Hermit, "Tell me about the tower", npc);
        var third = await NpcConversationOrchestrator.ConverseAsync(game, AdventureWorldFactory.Hermit, "What lies beyond?", npc);

        first.Success.Should().BeTrue();
        second.Success.Should().BeTrue();
        third.Success.Should().BeTrue();
        game.GetNpc(AdventureWorldFactory.Hermit).ConversationMemory.Should().HaveCount(3);
        game.Player.KnownClues.Should().Contain(AdventureWorldFactory.TowerClueKey);
        game.Player.KnownClues.Should().NotContain("outside-knowledge");
    }

    private sealed class ScriptedNpcAgent(IEnumerable<NpcResponse> responses) : INpcAgent
    {
        private readonly Queue<NpcResponse> _responses = new(responses);

        public Task<NpcResponse> ReplyAsync(NpcContext context, CancellationToken cancellationToken = default) => Task.FromResult(_responses.Dequeue());
    }
}