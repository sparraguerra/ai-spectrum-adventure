namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;
using Xunit;

/// <summary>Confirms free-form input never throws — worst case it resolves to Unknown, per FR-006.</summary>
public class InvalidInputHandlingTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!???")]
    [InlineData("qwertyuiopasdfghjkl")]
    public void Classify_NeverThrows_ForInvalidOrEmptyInput(string input)
    {
        var act = () => KeywordFallbackClassifier.Classify(input);

        act.Should().NotThrow();
    }

    [Fact]
    public void Classify_UnparseableInput_ReturnsUnknownAction_NotAnException()
    {
        var intent = KeywordFallbackClassifier.Classify("qwertyuiopasdfghjkl");

        intent.Action.Should().Be(IntentAction.Unknown);
    }
}
