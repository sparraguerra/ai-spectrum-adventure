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

        [Fact]
        public void CreateContext_IncludesAuthoredNpcGoalsAndRelationships()
        {
                const string definition = """
                {
                    "id": "npc-context",
                    "title": "NPC Context",
                    "startingLocationId": "harbor",
                    "locations": [{ "id": "harbor", "npcIds": ["keeper"] }],
                    "items": [],
                    "npcs": [{
                        "id": "keeper",
                        "name": "Keeper",
                        "personality": "Careful and kind.",
                        "knowledgeBoundary": ["harbor-secret"],
                        "goals": ["Protect the lighthouse"],
                        "relationships": ["Trusts the harbor master"]
                    }],
                    "puzzle": { "id": "gate", "requiredItemId": "key", "requiredClueKey": "" }
                }
                """;
                var game = AdventureWorldFactory.CreateNewGameFromJson(GameId.New(), DateTimeOffset.UnixEpoch, definition);
                var npc = game.GetNpc(new NpcId("keeper"));

                var context = NpcConversationOrchestrator.CreateContext(game, npc, "What do you want?");

                context.Goals.Should().ContainSingle().Which.Should().Be("Protect the lighthouse");
                context.Relationships.Should().ContainSingle().Which.Should().Be("Trusts the harbor master");
        }
}