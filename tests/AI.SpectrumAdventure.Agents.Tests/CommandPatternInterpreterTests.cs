namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;
using Xunit;

public class CommandPatternInterpreterTests
{
    [Theory]
    [InlineData("look")]
    [InlineData("look around")]
    [InlineData("l")]
    public void TryInterpret_LookVariants_ResolveToLookAction(string input)
    {
        var intent = CommandPatternInterpreter.TryInterpret(input);

        intent.Should().NotBeNull();
        intent!.Action.Should().Be(IntentAction.Look);
    }

    [Theory]
    [InlineData("go north", "North")]
    [InlineData("north", "North")]
    [InlineData("n", "North")]
    [InlineData("walk east", "East")]
    public void TryInterpret_DirectionVariants_ResolveToGoWithCanonicalDirection(string input, string expectedDirection)
    {
        var intent = CommandPatternInterpreter.TryInterpret(input);

        intent.Should().NotBeNull();
        intent!.Action.Should().Be(IntentAction.Go);
        intent.Target.Should().Be(expectedDirection);
    }

    [Theory]
    [InlineData("take key", "bridge-key")]
    [InlineData("grab the bridge key", "bridge-key")]
    [InlineData("pick up the key", "bridge-key")]
    public void TryInterpret_TakeSynonyms_ResolveToTakeWithCanonicalItem(string input, string expectedItem)
    {
        var intent = CommandPatternInterpreter.TryInterpret(input);

        intent.Should().NotBeNull();
        intent!.Action.Should().Be(IntentAction.Take);
        intent.Target.Should().Be(expectedItem);
    }

    [Fact]
    public void TryInterpret_AuthoredItemName_ResolvesToStableItemId()
    {
        var intent = CommandPatternInterpreter.TryInterpret("Take Screwdriver");

        intent.Should().NotBeNull();
        intent!.Action.Should().Be(IntentAction.Take);
        intent.Target.Should().Be("screwdriver");
    }

    [Fact]
    public void TryInterpret_ExamineDoor_ResolvesToExamineTowerEntrance()
    {
        var intent = CommandPatternInterpreter.TryInterpret("examine door");

        intent.Should().NotBeNull();
        intent!.Action.Should().Be(IntentAction.Examine);
        intent.Target.Should().Be("forgotten-tower-entrance");
    }

    [Fact]
    public void TryInterpret_UseKeyOnDoor_ResolvesToUseWithOnParameter()
    {
        var intent = CommandPatternInterpreter.TryInterpret("use key on door");

        intent.Should().NotBeNull();
        intent!.Action.Should().Be(IntentAction.Use);
        intent.Target.Should().Be("bridge-key");
        intent.Parameters.Should().ContainKey("on").WhoseValue.Should().Be("forgotten-tower-entrance");
    }

    [Fact]
    public void TryInterpret_QuotedNpcDialogue_ResolvesNpcAndCapturesUtterance()
    {
        var intent = CommandPatternInterpreter.TryInterpret("Talk to the hermit \"I want to go to the tower\"");

        intent.Should().NotBeNull();
        intent!.Action.Should().Be(IntentAction.TalkTo);
        intent.Target.Should().Be("hermit");
        intent.Parameters.Should().ContainKey("utterance").WhoseValue.Should().Be("I want to go to the tower");
    }

    [Fact]
    public void TryInterpret_UnrecognizableGibberish_ReturnsNull()
    {
        CommandPatternInterpreter.TryInterpret("asdlkjqwoiuxpvz").Should().BeNull();
    }
}
