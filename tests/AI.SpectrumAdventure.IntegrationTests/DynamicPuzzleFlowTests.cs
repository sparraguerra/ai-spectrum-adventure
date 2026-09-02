namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Puzzles;
using FluentAssertions;

public sealed class DynamicPuzzleFlowTests
{
    [Fact]
    public void TwoPuzzleFlow_AcceptsNaturalLanguageAndPersistsDeclaredFollowOnPuzzle()
    {
        var baseGame = E2ETestSupport.CreateStartedGame();
        var followOn = new Puzzle(
            new PuzzleId("tower-heart"),
            [new PuzzleSolutionDefinition("key", [new PuzzleCondition(PuzzleConditionType.ItemPossessed, AdventureWorldFactory.BridgeKey.Value)])],
            [new PuzzlePrerequisiteReference(PuzzleConditionType.PuzzleSolved, AdventureWorldFactory.ForgottenTowerEntrance.Value)],
            [new PuzzleOutcomeDefinition("reveal-archive", PuzzleOutcomeType.Location, "tower-archive")]);
        var game = new Game(baseGame.Id, baseGame.CreatedAt, baseGame.Player, baseGame.Locations, baseGame.Items, baseGame.Npcs, baseGame.Puzzle, puzzles: [baseGame.Puzzle, followOn]);
        game.Apply(new ItemTakenEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.BridgeKey, AdventureWorldFactory.OldBridge));
        E2ETestSupport.GrantTowerClue(game);

        var first = AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Use, AdventureWorldFactory.BridgeKey.Value, new Dictionary<string, string> { ["on"] = AdventureWorldFactory.ForgottenTowerEntrance.Value }));
        var second = AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Use, AdventureWorldFactory.BridgeKey.Value, new Dictionary<string, string> { ["on"] = "tower-heart" }));
        var rehydrated = Game.FromSnapshot(game.ToSnapshot());

        first.Success.Should().BeTrue();
        second.Success.Should().BeTrue();
        second.PuzzleEvaluation.Should().NotBeNull();
        second.PuzzleEvaluation!.ConsequenceIds.Should().ContainSingle().Which.Should().Be("reveal-archive");
        rehydrated.GetPuzzle(new PuzzleId("tower-heart")).Solved.Should().BeTrue();
    }
}