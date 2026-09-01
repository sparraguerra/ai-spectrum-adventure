namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Rules;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

public class RulesEngineTests
{
    private static Game CreateGame() => AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);

    private static ParsedIntent Intent(IntentAction action, string? target, Dictionary<string, string>? parameters = null) =>
        new(action, target, parameters ?? new Dictionary<string, string>(), Confidence: 1.0, RawInput: target ?? string.Empty);

    [Fact]
    public void Validate_Look_AlwaysSucceeds()
    {
        RulesEngine.Validate(Intent(IntentAction.Look, null), CreateGame()).Success.Should().BeTrue();
    }

    [Fact]
    public void Validate_Go_ValidDirection_Succeeds()
    {
        RulesEngine.Validate(Intent(IntentAction.Go, "North"), CreateGame()).Success.Should().BeTrue();
    }

    [Fact]
    public void Validate_Take_ItemNotPresent_FailsWithTargetNotPresent()
    {
        var result = RulesEngine.Validate(Intent(IntentAction.Take, AdventureWorldFactory.BridgeKey.Value), CreateGame());

        result.Reason.Should().Be(RulesFailureReason.TargetNotPresent);
    }

    [Fact]
    public void Validate_Use_ItemNotInInventory_FailsWithItemNotInInventory()
    {
        var result = RulesEngine.Validate(Intent(IntentAction.Use, AdventureWorldFactory.BridgeKey.Value), CreateGame());

        result.Reason.Should().Be(RulesFailureReason.ItemNotInInventory);
    }

    [Fact]
    public void Validate_UnknownAction_FailsWithUnknownAction()
    {
        var result = RulesEngine.Validate(ParsedIntent.Unknown("asdlkjasd"), CreateGame());

        result.Success.Should().BeFalse();
        result.Reason.Should().Be(RulesFailureReason.UnknownAction);
    }

    [Fact]
    public void Validate_TalkTo_NpcPresent_Succeeds()
    {
        var game = CreateGame();
        game.Apply(new Domain.Events.PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));

        var result = RulesEngine.Validate(Intent(IntentAction.TalkTo, AdventureWorldFactory.Hermit.Value), game);

        result.Success.Should().BeTrue();
    }
}
