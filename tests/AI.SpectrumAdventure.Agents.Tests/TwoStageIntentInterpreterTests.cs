namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;
using Xunit;

public class TwoStageIntentInterpreterTests
{
    private readonly TwoStageIntentInterpreter _interpreter = new();
    private readonly IReadOnlyDictionary<string, string> _harborVocabulary = new Dictionary<string, string>
    {
        ["sluice"] = "submerged-sluice",
        ["submerged sluice"] = "submerged-sluice",
        ["counterweight"] = "bronze-counterweight",
        ["bronze counterweight"] = "bronze-counterweight",
    };

    [Fact]
    public async Task InterpretAsync_ClassicCommand_UsesDeterministicPathWithFullConfidence()
    {
        var intent = await _interpreter.InterpretAsync("go north");

        intent.Action.Should().Be(IntentAction.Go);
        intent.Confidence.Should().Be(1.0);
    }

    [Fact]
    public async Task InterpretAsync_BackCommand_ReturnsTargetlessBackIntent()
    {
        var intent = await _interpreter.InterpretAsync("back");

        intent.Action.Should().Be(IntentAction.Back);
        intent.Target.Should().BeNull();
        intent.Confidence.Should().Be(1.0);
    }

    [Fact]
    public async Task InterpretAsync_NaturalLanguageInspection_FallsBackAndResolvesTheTarget()
    {
        var intent = await _interpreter.InterpretAsync("I carefully inspect the submerged sluice for traps.", _harborVocabulary);

        intent.Action.Should().Be(IntentAction.Examine);
        intent.Target.Should().Be("submerged-sluice");
    }

    [Fact]
    public async Task InterpretAsync_NaturalLanguageUseWithTarget_ResolvesItemAndOnTarget()
    {
        var intent = await _interpreter.InterpretAsync("I try to open the sluice using the bronze counterweight.", _harborVocabulary);

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
