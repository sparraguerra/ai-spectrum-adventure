namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Agents.Tests.TestDoubles;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;

public sealed class IntentInterpreterAgentTests
{
    private static readonly IReadOnlyDictionary<string, string> TargetVocabulary = new Dictionary<string, string>
    {
        ["key"] = "rusty-key",
        ["rusty key"] = "rusty-key",
        ["sluice"] = "submerged-sluice",
    };

    [Fact]
    public async Task InterpretAsync_ResolvesSupportedIntentAndProvidesActionAndTargetContext()
    {
        const string input = "Could I snag the rusty key?";
        var expected = new ParsedIntent(IntentAction.Take, "rusty-key", new Dictionary<string, string>(), 0.91, input);
        var runner = new FakeAgentRunner().EnqueueResult(expected);
        var agent = new IntentInterpreterAgent(runner);

        var actual = await agent.InterpretAsync(input, TargetVocabulary);

        actual.Should().BeEquivalentTo(expected);
        runner.CallCount.Should().Be(1);
        runner.LastPrompt.Should().Contain(input)
            .And.Contain("rusty-key")
            .And.Contain("grab")
            .And.Contain("return")
            .And.Contain("Unknown");
    }

    [Fact]
    public async Task InterpretAsync_RejectsTargetOutsideAdventureVocabulary()
    {
        const string input = "take the moon";
        var runner = new FakeAgentRunner().EnqueueResult(
            new ParsedIntent(IntentAction.Take, "moon", new Dictionary<string, string>(), 0.9, input));
        var agent = new IntentInterpreterAgent(runner);

        var actual = await agent.InterpretAsync(input, TargetVocabulary);

        actual.Action.Should().Be(IntentAction.Unknown);
        actual.Target.Should().BeNull();
        actual.RawInput.Should().Be(input);
    }

    [Fact]
    public async Task InterpretAsync_RejectsUnsupportedActionValue()
    {
        const string input = "do something magical";
        var runner = new FakeAgentRunner().EnqueueResult(
            new ParsedIntent((IntentAction)999, null, new Dictionary<string, string>(), 0.9, input));
        var agent = new IntentInterpreterAgent(runner);

        var actual = await agent.InterpretAsync(input, TargetVocabulary);

        actual.Should().BeEquivalentTo(ParsedIntent.Unknown(input));
    }

    [Fact]
    public async Task InterpretAsync_PreservesUnknownWhenModelFindsNoSupportedIntent()
    {
        const string input = "The key was mentioned in a story.";
        var runner = new FakeAgentRunner().EnqueueResult(ParsedIntent.Unknown("model text"));
        var agent = new IntentInterpreterAgent(runner);

        var actual = await agent.InterpretAsync(input, TargetVocabulary);

        actual.Should().BeEquivalentTo(ParsedIntent.Unknown(input));
    }

    [Fact]
    public async Task InterpretAsync_ReturnsUnknownAfterModelFailures()
    {
        const string input = "I could use the rusty key here.";
        var runner = new FakeAgentRunner()
            .EnqueueFailure(new InvalidOperationException("temporary model failure"))
            .EnqueueFailure(new InvalidOperationException("model unavailable"));
        var agent = new IntentInterpreterAgent(runner);

        var actual = await agent.InterpretAsync(input, TargetVocabulary);

        actual.Should().BeEquivalentTo(ParsedIntent.Unknown(input));
        runner.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task InterpretAsync_PropagatesCancellation()
    {
        var runner = new FakeAgentRunner();
        var agent = new IntentInterpreterAgent(runner);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var act = () => agent.InterpretAsync("take the key", TargetVocabulary, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        runner.CallCount.Should().Be(0);
    }
}
