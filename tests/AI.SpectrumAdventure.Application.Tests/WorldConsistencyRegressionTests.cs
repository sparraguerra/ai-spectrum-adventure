namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Application.Rules;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;
using FluentAssertions;

public sealed class WorldConsistencyRegressionTests
{
    [Fact]
    public void Validate_RejectsContradictoryCandidateWithoutChangingAuthoritativeWorld()
    {
        var regionId = new RegionId("forest");
        var start = new WorldLocation(new LocationId("start"), regionId, LocationType.Path, "start", "A forest path.", new EnvironmentalProperties(TerrainKind.Forest, "temperate", "daylight"));
        var world = new World(WorldId.New(), new WorldSeed("seed"), new GenerationVersion("1"), [new Region(regionId, RegionType.Wilderness, [TerrainKind.Forest], new RegionExpansionPolicy(2), [start.Id])], [start], []);
        var candidate = new WorldExpansionCandidate(null, new WorldLocation(new LocationId("new"), regionId, LocationType.Path, "new", "A water path.", new EnvironmentalProperties(TerrainKind.Water, "temperate", "daylight")), new WorldConnection(new ConnectionId("new-connection"), start.Id, new LocationId("new"), ConnectionDirection.South), new GeneratedContentMetadata(new GenerationKey("key"), world.Seed, world.GenerationVersion, start.Id, ConnectionDirection.South, 0, "test", DateTimeOffset.UnixEpoch));

        var result = new WorldConstraintValidator().Validate(world, candidate);

        result.IsValid.Should().BeFalse();
        world.Locations.Should().ContainSingle().Which.Id.Should().Be(start.Id);
        world.Events.Should().BeEmpty();
    }

    [Fact]
    public void Validate_RejectsContradictoryEnrichmentOutput()
    {
        var context = new WorldEnrichmentContext(Guid.NewGuid(), "forest-path", "forest", 1, "Forest Path", "A factual path.", ["known-fact"], ["trees"]);
        var proposal = new WorldEnrichmentProposal("other-path", 2, "Changed", "A claim.", "ominous", ["castle"], ["forbidden-fact"]);

        new WorldEnrichmentValidator().Validate(context, proposal).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateContext_ExcludesUnauthorizedNpcKnowledgeEvenWhenPlayerKnowsIt()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UnixEpoch);
        var npc = game.GetNpc(AdventureWorldFactory.Hermit);
        game.PlayerKnowledge.DiscoverLore(new LoreId("private-action"), DateTimeOffset.UnixEpoch);
        game.PlayerKnowledge.DiscoverLore(new LoreId(AdventureWorldFactory.TowerClueKey), DateTimeOffset.UnixEpoch);

        var context = NpcConversationOrchestrator.CreateContext(game, npc, "What happened elsewhere?");

        context.PlayerKnownFacts.Should().Contain(AdventureWorldFactory.TowerClueKey);
        context.PlayerKnownFacts.Should().NotContain("private-action");
    }

    [Fact]
    public void ProcessAction_RejectsImpossibleActionWithoutChangingGameState()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UnixEpoch);
        var before = game.ToSnapshot();

        var result = AdventureOrchestrator.ProcessAction(game, new ParsedIntent(IntentAction.Take, "missing-item", new Dictionary<string, string>(), 1, "take missing item"));

        result.Success.Should().BeFalse();
        game.ToSnapshot().Should().BeEquivalentTo(before);
    }

    [Fact]
    public void Evaluate_RejectsPuzzleBypassAndRepeatedAction()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UnixEpoch);
        var bypass = PuzzleRules.Evaluate(new ParsedIntent(IntentAction.Use, AdventureWorldFactory.BridgeKey.Value, new Dictionary<string, string> { ["on"] = AdventureWorldFactory.ForgottenTowerEntrance.Value }, 1, "use key"), game);
        game.Apply(new ItemTakenEvent(Guid.NewGuid(), DateTimeOffset.UnixEpoch, AdventureWorldFactory.BridgeKey, AdventureWorldFactory.OldBridge));
        game.Apply(new NpcRelationshipChangedEvent(Guid.NewGuid(), DateTimeOffset.UnixEpoch, AdventureWorldFactory.Hermit, WorldFlags.NpcGaveClue, "Ask about the tower.", "Only a patient hand may open it."));
        var accepted = PuzzleRules.Evaluate(new ParsedIntent(IntentAction.Use, AdventureWorldFactory.BridgeKey.Value, new Dictionary<string, string> { ["on"] = AdventureWorldFactory.ForgottenTowerEntrance.Value }, 1, "use key"), game);
        game.Apply(new PuzzleSolvedEvent(Guid.NewGuid(), DateTimeOffset.UnixEpoch, AdventureWorldFactory.ForgottenTowerEntrance));
        var repeated = PuzzleRules.Evaluate(new ParsedIntent(IntentAction.Use, AdventureWorldFactory.BridgeKey.Value, new Dictionary<string, string> { ["on"] = AdventureWorldFactory.ForgottenTowerEntrance.Value }, 1, "use key"), game);

        bypass.Accepted.Should().BeFalse();
        accepted.Accepted.Should().BeTrue();
        repeated.Accepted.Should().BeFalse();
    }
}