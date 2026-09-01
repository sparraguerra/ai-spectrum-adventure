namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Npcs;
using FluentAssertions;
using Xunit;

public class NpcTests
{
    [Fact]
    public void Knows_ReturnsTrue_ForWhitelistedTopic()
    {
        var npc = new Npc(new NpcId("hermit"), "The Hermit", "cryptic", knowledgeBoundary: ["tower-clue"]);

        npc.Knows("tower-clue").Should().BeTrue();
    }

    [Fact]
    public void Knows_ReturnsFalse_ForTopicOutsideKnowledgeBoundary()
    {
        var npc = new Npc(new NpcId("hermit"), "The Hermit", "cryptic", knowledgeBoundary: ["tower-clue"]);

        npc.Knows("secret-treasure-location").Should().BeFalse();
    }

    [Fact]
    public void RecordConversationTurn_AppendsToConversationMemory_InOrder()
    {
        var npc = new Npc(new NpcId("hermit"), "The Hermit", "cryptic", knowledgeBoundary: ["tower-clue"]);
        var now = DateTimeOffset.UtcNow;

        npc.RecordConversationTurn("Hello?", "...", now);
        npc.RecordConversationTurn("What do you know of the tower?", "Beware.", now.AddSeconds(1));

        npc.ConversationMemory.Should().HaveCount(2);
        npc.ConversationMemory.Last().NpcReply.Should().Be("Beware.");
    }
}
