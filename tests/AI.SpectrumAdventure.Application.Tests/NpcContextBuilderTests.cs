namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

public sealed class NpcContextBuilderTests
{
    [Fact]
    public void CreateContext_ExcludesPrivateActionsDistantFactsAndUnauthorizedLore()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UnixEpoch);
        var npc = game.GetNpc(AdventureWorldFactory.Hermit);
        game.PlayerKnowledge.DiscoverLore(new LoreId("forbidden-distant-lore"), DateTimeOffset.UnixEpoch);
        game.PlayerKnowledge.DiscoverLore(new LoreId(AdventureWorldFactory.TowerClueKey), DateTimeOffset.UnixEpoch);

        var context = NpcConversationOrchestrator.CreateContext(game, npc, "What have I done elsewhere?");

        context.KnowledgeBoundary.Should().BeEquivalentTo([AdventureWorldFactory.TowerClueKey]);
        context.PlayerKnownFacts.Should().BeEquivalentTo([AdventureWorldFactory.TowerClueKey]);
        context.RecentConversation.Should().BeEmpty();
        context.PlayerKnownFacts.Should().NotContain("forbidden-distant-lore");
    }
}