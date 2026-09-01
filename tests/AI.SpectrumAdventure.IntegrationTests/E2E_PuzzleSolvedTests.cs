namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;

public sealed class E2E_PuzzleSolvedTests
{
    [Fact]
    public void Puzzle_RequiresKeyAndClue_ThenUnlocksTowerPersistently()
    {
        var game = E2ETestSupport.CreateStartedGame();
        AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Go, "north"));
        AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Go, "east"));
        AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Take, AdventureWorldFactory.BridgeKey.Value));
        AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Go, "west"));

        var beforeClue = AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Use, AdventureWorldFactory.BridgeKey.Value,
            new Dictionary<string, string> { ["on"] = AdventureWorldFactory.ForgottenTowerEntrance.Value }));
        E2ETestSupport.GrantTowerClue(game);
        var solve = AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Use, AdventureWorldFactory.BridgeKey.Value,
            new Dictionary<string, string> { ["on"] = AdventureWorldFactory.ForgottenTowerEntrance.Value }));
        var enter = AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Go, "west"));

        beforeClue.Success.Should().BeFalse();
        solve.Success.Should().BeTrue();
        enter.Success.Should().BeTrue();
        game.Puzzle.Solved.Should().BeTrue();
        game.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.ForgottenTower);
    }
}