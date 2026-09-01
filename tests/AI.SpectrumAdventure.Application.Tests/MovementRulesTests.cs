namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Rules;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

public class MovementRulesTests
{
    private static Game CreateGame() => AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);

    [Fact]
    public void Validate_ValidDirection_SucceedsWithPlayerMovedAndLocationDiscoveredEvents()
    {
        var game = CreateGame();

        var result = MovementRules.Validate("North", game);

        result.Success.Should().BeTrue();
        result.ProposedEvents.Should().ContainSingle(e => e is PlayerMovedEvent);
        result.ProposedEvents.Should().ContainSingle(e => e is LocationDiscoveredEvent);
    }

    [Fact]
    public void Validate_UnknownDirection_FailsWithNoSuchExit()
    {
        var game = CreateGame();

        var result = MovementRules.Validate("South", game);

        result.Success.Should().BeFalse();
        result.Reason.Should().Be(RulesFailureReason.NoSuchExit);
        result.ProposedEvents.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ForgottenTowerEntrance_FailsWithExitConditionNotMet_BeforePuzzleSolved()
    {
        var game = CreateGame();
        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));

        var result = MovementRules.Validate("West", game);

        result.Success.Should().BeFalse();
        result.Reason.Should().Be(RulesFailureReason.ExitConditionNotMet);
    }

    [Fact]
    public void Validate_ForgottenTowerEntrance_SucceedsOncePuzzleSolved()
    {
        var game = CreateGame();
        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));
        game.Apply(new PuzzleSolvedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForgottenTowerEntrance));

        var result = MovementRules.Validate("West", game);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Validate_RepeatedInvalidMovement_ReturnsTheSameFailureReasonEachTime()
    {
        var game = CreateGame();

        var first = MovementRules.Validate("South", game);
        var second = MovementRules.Validate("South", game);

        first.Reason.Should().Be(second.Reason);
        first.Success.Should().Be(second.Success);
    }
}
