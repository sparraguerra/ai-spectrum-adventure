namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

/// <summary>Edge Case "Ambiguous Action": a recognized action with no resolvable target asks a clarifying question.</summary>
public class AmbiguousActionTests
{
    [Fact]
    public void ProcessAction_ExamineWithNoTarget_ReturnsAmbiguousIntentAskingWhatToExamine()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var intent = new ParsedIntent(IntentAction.Examine, null, new Dictionary<string, string>(), 0.3, "examine the thing");

        var result = AdventureOrchestrator.ProcessAction(game, intent);

        result.Success.Should().BeFalse();
        result.Reason.Should().Be("AmbiguousIntent");
        result.Narrative.Should().Contain("examine");
    }

    [Fact]
    public void ProcessAction_UnknownAction_AsksForClarificationWithoutChangingLocation()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var currentLocationId = game.Player.CurrentLocationId;

        var result = AdventureOrchestrator.ProcessAction(game, ParsedIntent.Unknown("gibberish"));

        result.Success.Should().BeFalse();
        result.Reason.Should().Be("UnknownAction");
        result.Narrative.Should().Contain("clarify");
        game.Player.CurrentLocationId.Should().Be(currentLocationId);
    }
}
