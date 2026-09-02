namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Puzzles;
using AI.SpectrumAdventure.Application.Rules;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Puzzles;
using FluentAssertions;

public sealed class PuzzleChainTests
{
    [Fact]
    public void Evaluate_FollowOnPuzzleAcceptsOnlyAfterDeclaredPrerequisiteAndReturnsDeclaredOutcomes()
    {
        var game = CreateGameWithFollowOnPuzzle();
        game.Player.Inventory.Add(AdventureWorldFactory.BridgeKey);
        var intent = new ParsedIntent(IntentAction.Use, AdventureWorldFactory.BridgeKey.Value, new Dictionary<string, string> { ["on"] = "tower-heart" }, 1, "use key on heart");

        var beforePrerequisite = PuzzleRules.Evaluate(intent, game);
        game.Apply(new PuzzleSolvedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForgottenTowerEntrance));
        var accepted = PuzzleRules.Evaluate(intent, game);
        var outcomes = new ApplyPuzzleOutcomeUseCase().Execute(game.GetPuzzle(new PuzzleId("tower-heart")), accepted);

        beforePrerequisite.Accepted.Should().BeFalse();
        accepted.Accepted.Should().BeTrue();
        outcomes.Should().ContainSingle().Which.Id.Should().Be("reveal-archive");
    }

    private static Game CreateGameWithFollowOnPuzzle()
    {
        var baseGame = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var followOn = new Puzzle(
            new PuzzleId("tower-heart"),
            [new PuzzleSolutionDefinition("bridge-key", [new PuzzleCondition(PuzzleConditionType.ItemPossessed, AdventureWorldFactory.BridgeKey.Value)])],
            [new PuzzlePrerequisiteReference(PuzzleConditionType.PuzzleSolved, AdventureWorldFactory.ForgottenTowerEntrance.Value)],
            [new PuzzleOutcomeDefinition("reveal-archive", PuzzleOutcomeType.Location, "tower-archive")]);
        return new Game(GameId.New(), baseGame.CreatedAt, baseGame.Player, baseGame.Locations, baseGame.Items, baseGame.Npcs, baseGame.Puzzle, puzzles: [baseGame.Puzzle, followOn]);
    }
}