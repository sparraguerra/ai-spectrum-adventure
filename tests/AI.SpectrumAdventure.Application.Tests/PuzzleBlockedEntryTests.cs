namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

/// <summary>US9 acceptance #1/#5: the tower entrance is consistently blocked until both puzzle conditions are met.</summary>
public class PuzzleBlockedEntryTests
{
    private static ParsedIntent Go(string direction) =>
        new(IntentAction.Go, direction, new Dictionary<string, string>(), 1.0, direction);

    [Fact]
    public void RepeatedAttemptsToEnterTower_WithoutSolvingPuzzle_AreAllConsistentlyBlocked()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        AdventureOrchestrator.ProcessAction(game, Go("North"));

        for (var i = 0; i < 3; i++)
        {
            var attempt = AdventureOrchestrator.ProcessAction(game, Go("West"));
            attempt.Success.Should().BeFalse();
            attempt.Reason.Should().Be("ExitConditionNotMet");
        }

        game.Puzzle.Solved.Should().BeFalse();
    }
}
