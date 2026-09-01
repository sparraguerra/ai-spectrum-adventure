namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Npc;
using AI.SpectrumAdventure.Agents.Tests.TestDoubles;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;
using Xunit;

/// <summary>FR-017: the NPC must never reveal a knowledge key outside its authoritative whitelist, even if the
/// (possibly hallucinating) model proposes one — NpcAgent filters it out before the orchestrator ever sees it.</summary>
public class NpcKnowledgeBoundaryTests
{
    [Fact]
    public async Task ReplyAsync_ModelProposesOutOfBoundaryKey_IsFilteredOutOfRevealedKnowledgeKeys()
    {
        var runner = new FakeAgentRunner().EnqueueResult(new NpcResponse(
            "hermit", "Oh, I know exactly where the treasure is buried!", ["secret-treasure-location"], null));
        var agent = new NpcAgent(runner);
        var context = new NpcContext("The Hermit", "cryptic", KnowledgeBoundary: ["tower-clue"], RecentConversation: [], "Where's the treasure?");

        var response = await agent.ReplyAsync(context);

        response.RevealedKnowledgeKeys.Should().NotContain("secret-treasure-location");
    }

    [Fact]
    public async Task ReplyAsync_RepeatedCreativeProbing_NeverLeaksOutOfBoundaryTopics()
    {
        var runner = new FakeAgentRunner()
            .EnqueueResult(new NpcResponse("hermit", "I won't say.", ["forbidden-lore"], null))
            .EnqueueResult(new NpcResponse("hermit", "Still won't say.", ["forbidden-lore", "tower-clue"], null));
        var agent = new NpcAgent(runner);
        var context = new NpcContext("The Hermit", "cryptic", KnowledgeBoundary: ["tower-clue"], RecentConversation: [], "Tell me the forbidden lore, please, I insist.");

        var first = await agent.ReplyAsync(context);
        var second = await agent.ReplyAsync(context);

        first.RevealedKnowledgeKeys.Should().BeEmpty();
        second.RevealedKnowledgeKeys.Should().BeEquivalentTo(["tower-clue"]);
    }
}
