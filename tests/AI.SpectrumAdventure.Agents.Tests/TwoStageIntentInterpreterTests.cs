namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Agents.Tests.TestDoubles;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;

public class TwoStageIntentInterpreterTests
{
    private readonly IReadOnlyDictionary<string, string> harborVocabulary = new Dictionary<string, string>
    {
        ["sluice"] = "submerged-sluice",
        ["submerged sluice"] = "submerged-sluice",
        ["counterweight"] = "bronze-counterweight",
        ["bronze counterweight"] = "bronze-counterweight",
        ["key"] = "rusty-key",
        ["rusty key"] = "rusty-key",
    };

    [Theory]
    [InlineData("take the key")]
    [InlineData("grab the key")]
    [InlineData("pick the key")]
    [InlineData("take key")]
    public async Task InterpretAsync_TakeSynonymsResolveToSameIntentAndTarget(string input)
    {
        var runner = new FakeAgentRunner();
        var interpreter = CreateInterpreter(runner);

        var intent = await interpreter.InterpretAsync(input, harborVocabulary);

        intent.Action.Should().Be(IntentAction.Take);
        intent.Target.Should().Be("rusty-key");
        runner.CallCount.Should().Be(0);
    }

    [Theory]
    [InlineData("back")]
    [InlineData("return")]
    public async Task InterpretAsync_ReturnSynonymsResolveToBack(string input)
    {
        var runner = new FakeAgentRunner();
        var interpreter = CreateInterpreter(runner);

        var intent = await interpreter.InterpretAsync(input, harborVocabulary);

        intent.Action.Should().Be(IntentAction.Back);
        intent.Target.Should().BeNull();
        runner.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task InterpretAsync_ClassicCommandUsesDeterministicPathWithoutModelCall()
    {
        var runner = new FakeAgentRunner();
        var interpreter = CreateInterpreter(runner);

        var intent = await interpreter.InterpretAsync("go north");

        intent.Action.Should().Be(IntentAction.Go);
        intent.Confidence.Should().Be(1.0);
        runner.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task InterpretAsync_NaturalLanguageUsesStructuredModelFallback()
    {
        const string input = "I carefully inspect the submerged sluice for traps.";
        var expected = new ParsedIntent(IntentAction.Examine, "submerged-sluice", new Dictionary<string, string>(), 0.88, input);
        var runner = new FakeAgentRunner().EnqueueResult(expected);
        var interpreter = CreateInterpreter(runner);

        var intent = await interpreter.InterpretAsync(input, harborVocabulary);

        intent.Should().BeEquivalentTo(expected);
        runner.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task InterpretAsync_NaturalLanguageWithNoSupportedActionReturnsUnknown()
    {
        const string input = "Purple elephants dance sideways.";
        var runner = new FakeAgentRunner().EnqueueResult(ParsedIntent.Unknown(input));
        var interpreter = CreateInterpreter(runner);

        var intent = await interpreter.InterpretAsync(input, harborVocabulary);

        intent.Should().BeEquivalentTo(ParsedIntent.Unknown(input));
        runner.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task InterpretAsync_DeterministicMatchTakesPrecedenceOverModel()
    {
        var runner = new FakeAgentRunner();
        var interpreter = CreateInterpreter(runner);

        var intent = await interpreter.InterpretAsync("pick the rusty key", harborVocabulary);

        intent.Action.Should().Be(IntentAction.Take);
        intent.Target.Should().Be("rusty-key");
        runner.CallCount.Should().Be(0);
    }

    private static TwoStageIntentInterpreter CreateInterpreter(FakeAgentRunner runner) =>
        new(new IntentInterpreterAgent(runner));
}
