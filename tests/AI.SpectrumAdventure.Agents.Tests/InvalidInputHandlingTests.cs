namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Agents.Tests.TestDoubles;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;

public class InvalidInputHandlingTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!???")]
    [InlineData("qwertyuiopasdfghjkl")]
    public async Task InterpretAsync_NeverThrows_ForInvalidOrEmptyInput(string input)
    {
        var runner = new FakeAgentRunner().EnqueueResult(ParsedIntent.Unknown(input));
        var interpreter = new TwoStageIntentInterpreter(new IntentInterpreterAgent(runner));

        var act = async () => await interpreter.InterpretAsync(input);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InterpretAsync_UnparseableInputReturnsUnknown()
    {
        const string input = "qwertyuiopasdfghjkl";
        var runner = new FakeAgentRunner().EnqueueResult(ParsedIntent.Unknown(input));
        var interpreter = new TwoStageIntentInterpreter(new IntentInterpreterAgent(runner));

        var intent = await interpreter.InterpretAsync(input);

        intent.Action.Should().Be(IntentAction.Unknown);
    }
}
