namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;
using Xunit;

public class CommandPatternInterpreterTests
{
    private static readonly WorldVocabulary HarborVocabulary = new(new Dictionary<string, string>
    {
        ["buoy"] = "rusted-buoy",
        ["rusted buoy"] = "rusted-buoy",
        ["counterweight"] = "bronze-counterweight",
        ["bronze counterweight"] = "bronze-counterweight",
        ["sluice"] = "submerged-sluice",
        ["ines"] = "ines-the-lighthouse-keeper",
        ["lighthouse keeper"] = "ines-the-lighthouse-keeper",
    });

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
    [InlineData("take counterweight", "bronze-counterweight")]
    [InlineData("grab the bronze counterweight", "bronze-counterweight")]
    [InlineData("pick up the counterweight", "bronze-counterweight")]
    public void TryInterpret_TakeSynonyms_ResolveToTakeWithCanonicalItem(string input, string expectedItem)
    {
        var intent = CommandPatternInterpreter.TryInterpret(input, HarborVocabulary);

        intent.Should().NotBeNull();
        intent!.Action.Should().Be(IntentAction.Take);
        intent.Target.Should().Be(expectedItem);
    }

    [Theory]
    [InlineData("drop counterweight")]
    [InlineData("leave the bronze counterweight")]
    public void TryInterpret_DropSynonyms_ResolveToDropWithCanonicalItem(string input)
    {
        var intent = CommandPatternInterpreter.TryInterpret(input, HarborVocabulary);

        Enum.TryParse<IntentAction>("Drop", out var dropAction).Should().BeTrue();
        intent.Should().NotBeNull();
        intent!.Action.Should().Be(dropAction);
        intent.Target.Should().Be("bronze-counterweight");
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
    public void TryInterpret_ExamineSluice_ResolvesToAdventurePuzzleTarget()
    {
        var intent = CommandPatternInterpreter.TryInterpret("examine the sluice", HarborVocabulary);

        intent.Should().NotBeNull();
        intent!.Action.Should().Be(IntentAction.Examine);
        intent.Target.Should().Be("submerged-sluice");
    }

    [Fact]
    public void TryInterpret_UseCounterweightOnSluice_ResolvesToUseWithOnParameter()
    {
        var intent = CommandPatternInterpreter.TryInterpret("use counterweight on sluice", HarborVocabulary);

        intent.Should().NotBeNull();
        intent!.Action.Should().Be(IntentAction.Use);
        intent.Target.Should().Be("bronze-counterweight");
        intent.Parameters.Should().ContainKey("on").WhoseValue.Should().Be("submerged-sluice");
    }

    [Fact]
    public void TryInterpret_QuotedNpcDialogue_ResolvesAdventureNpcAndCapturesUtterance()
    {
        var intent = CommandPatternInterpreter.TryInterpret("Talk to the lighthouse keeper \"What do you know about the buoy?\"", HarborVocabulary);

        intent.Should().NotBeNull();
        intent!.Action.Should().Be(IntentAction.TalkTo);
        intent.Target.Should().Be("ines-the-lighthouse-keeper");
        intent.Parameters.Should().ContainKey("utterance").WhoseValue.Should().Be("What do you know about the buoy?");
    }

    [Fact]
    public void TryInterpret_UnrecognizableGibberish_ReturnsNull()
    {
        CommandPatternInterpreter.TryInterpret("asdlkjqwoiuxpvz").Should().BeNull();
    }
}
