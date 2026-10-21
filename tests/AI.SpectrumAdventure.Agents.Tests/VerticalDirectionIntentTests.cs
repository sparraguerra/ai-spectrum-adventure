namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;
using Xunit;

public class VerticalDirectionIntentTests
{
    private readonly TwoStageIntentInterpreter _interpreter = new();

    [Theory]
    [InlineData("up", "Up")]
    [InlineData("DOWN", "Down")]
    [InlineData("go up", "Up")]
    [InlineData("GO DOWN", "Down")]
    [InlineData("walk Down", "Down")]
    [InlineData("travel up", "Up")]
    [InlineData("head down", "Down")]
    public async Task InterpretAsync_VerticalMovement_ResolvesCaseInsensitively(string input, string expectedDirection)
    {
        var intent = await _interpreter.InterpretAsync(input);

        intent.Action.Should().Be(IntentAction.Go);
        intent.Target.Should().Be(expectedDirection);
        intent.Confidence.Should().Be(1.0);
    }
}
