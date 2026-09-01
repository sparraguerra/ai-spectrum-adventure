namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;
using Xunit;

public class TwoStageIntentInterpreterTests
{
    private readonly TwoStageIntentInterpreter _interpreter = new();

    [Fact]
    public async Task InterpretAsync_ClassicCommand_UsesDeterministicPathWithFullConfidence()
    {
        var intent = await _interpreter.InterpretAsync("go north");

        intent.Action.Should().Be(IntentAction.Go);
        intent.Confidence.Should().Be(1.0);
    }

    [Fact]
    public async Task InterpretAsync_NaturalLanguageInspection_FallsBackAndResolvesTheTarget()
    {
        var intent = await _interpreter.InterpretAsync("I carefully inspect the door for traps.");

        intent.Action.Should().Be(IntentAction.Examine);
        intent.Target.Should().Be("forgotten-tower-entrance");
    }

    [Fact]
    public async Task InterpretAsync_NaturalLanguageUseWithTarget_ResolvesItemAndOnTarget()
    {
        var intent = await _interpreter.InterpretAsync("I try to open the chest using the iron key.");

        intent.Action.Should().Be(IntentAction.Open);
        intent.Target.Should().NotBeNull();
    }

    [Fact]
    public async Task InterpretAsync_CompletelyUnrelatedText_ReturnsUnknown()
    {
        var intent = await _interpreter.InterpretAsync("purple elephants dance sideways");

        intent.Action.Should().Be(IntentAction.Unknown);
        intent.Confidence.Should().Be(0);
    }
}
