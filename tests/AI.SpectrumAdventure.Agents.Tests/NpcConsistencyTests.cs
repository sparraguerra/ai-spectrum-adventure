namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Npc;
using AI.SpectrumAdventure.Agents.Tests.TestDoubles;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;
using Xunit;

public class NpcConsistencyTests
{
    private static NpcContext BaseContext(string utterance, IReadOnlyCollection<string>? recent = null) => new(
        "The Hermit",
        "A reclusive old hermit, cryptic and wary of strangers.",
        KnowledgeBoundary: ["tower-clue"],
        RecentConversation: recent ?? [],
        PlayerUtterance: utterance);

    [Fact]
    public async Task ReplyAsync_AcrossThreeTurns_RemainsConsistentWithPersonalityAndKnowledge()
    {
        var runner = new FakeAgentRunner()
            .EnqueueResult(new NpcResponse("hermit", "Who goes there?", [], null))
            .EnqueueResult(new NpcResponse("hermit", "The tower keeps its secrets from the careless.", [], ["tower-clue"]))
            .EnqueueResult(new NpcResponse("hermit", "Patience. You already know enough.", ["tower-clue"], null));
        var agent = new NpcAgent(runner);

        var turn1 = await agent.ReplyAsync(BaseContext("Hello?"));
        var turn2 = await agent.ReplyAsync(BaseContext("What do you know of the tower?"));
        var turn3 = await agent.ReplyAsync(BaseContext("Tell me more."));

        turn1.Reply.Should().NotBeNullOrWhiteSpace();
        turn2.Reply.Should().NotBeNullOrWhiteSpace();
        turn3.RevealedKnowledgeKeys.Should().Contain("tower-clue");
    }
}
