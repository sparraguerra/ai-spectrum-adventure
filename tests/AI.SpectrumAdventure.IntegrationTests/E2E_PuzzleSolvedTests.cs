namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;
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

    [Fact]
    public async Task CommandFlow_UsingBridgeKeyOnTowerDoor_AfterHermitClue_SolvesPuzzleAndUnlocksWestExit()
    {
        var game = E2ETestSupport.CreateStartedGame();
        var vocabulary = new WorldVocabulary(game.TargetVocabulary);

        AdventureOrchestrator.ProcessAction(game, Interpret("go north", vocabulary));
        AdventureOrchestrator.ProcessAction(game, Interpret("go east", vocabulary));
        var take = AdventureOrchestrator.ProcessAction(game, Interpret("take rusted bridge key", vocabulary));
        AdventureOrchestrator.ProcessAction(game, Interpret("go west", vocabulary));

        var hermit = new ClueNpcAgent();
        var conversation = await NpcConversationOrchestrator.ConverseAsync(
            game, AdventureWorldFactory.Hermit, "Tell me about the tower.", hermit);
        var solve = AdventureOrchestrator.ProcessAction(game, Interpret("use bridge key on tower door", vocabulary));
        var enter = AdventureOrchestrator.ProcessAction(game, Interpret("go west", vocabulary));

        take.Success.Should().BeTrue();
        conversation.Success.Should().BeTrue();
        game.Player.KnownClues.Should().Contain(AdventureWorldFactory.TowerClueKey);
        solve.Success.Should().BeTrue();
        game.Puzzle.Solved.Should().BeTrue();
        game.HasFlag(WorldFlags.TowerUnlocked).Should().BeTrue();
        enter.Success.Should().BeTrue();
        game.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.ForgottenTower);
    }

    private static ParsedIntent Interpret(string command, WorldVocabulary vocabulary) =>
        CommandPatternInterpreter.TryInterpret(command, vocabulary)
        ?? throw new InvalidOperationException($"Command '{command}' was not interpreted.");

    private sealed class ClueNpcAgent : INpcAgent
    {
        public Task<NpcResponse> ReplyAsync(NpcContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new NpcResponse(
                AdventureWorldFactory.Hermit.Value,
                "The tower answers only a patient hand.",
                [AdventureWorldFactory.TowerClueKey],
                null));
    }
}