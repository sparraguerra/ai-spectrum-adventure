namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Application.Rules;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

/// <summary>Edge Case "Puzzle Bypass Attempts": creative-but-incorrect attempts must not solve the puzzle.</summary>
public class PuzzleBypassAttemptTests
{
    [Fact]
    public void UsingTheWrongItemOnTheTowerEntrance_FailsWithoutSolvingThePuzzle()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        game.Apply(new Domain.Events.PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));
        game.Apply(new Domain.Events.ItemTakenEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.Sign, AdventureWorldFactory.ForestEntrance));

        var intent = new ParsedIntent(
            IntentAction.Use,
            AdventureWorldFactory.Sign.Value,
            new Dictionary<string, string> { ["on"] = AdventureWorldFactory.ForgottenTowerEntrance.Value },
            1.0,
            "use sign on tower door");

        var result = AdventureOrchestrator.ProcessAction(game, intent);

        result.Success.Should().BeFalse();
        result.Reason.Should().Be(RulesFailureReason.PuzzleConditionNotMet.ToString());
        game.Puzzle.Solved.Should().BeFalse();
    }
}
