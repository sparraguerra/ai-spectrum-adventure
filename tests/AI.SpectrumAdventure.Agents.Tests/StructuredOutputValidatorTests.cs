namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

public class StructuredOutputValidatorTests
{
    [Fact]
    public async Task ExecuteAsync_SuccessfulFirstAttempt_ReturnsAgentResult_WithoutFallback()
    {
        var runner = new FakeAgentRunner().EnqueueResult("first-try-result");

        var result = await StructuredOutputValidator.ExecuteAsync(
            ct => runner.RunAsync<string>("prompt", ct),
            fallback: () => "fallback");

        result.Should().Be("first-try-result");
        runner.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_FailsOnce_ThenSucceedsOnRetry_ReturnsRetryResult()
    {
        var runner = new FakeAgentRunner()
            .EnqueueFailure(new InvalidOperationException("schema invalid"))
            .EnqueueResult("retry-result");

        var result = await StructuredOutputValidator.ExecuteAsync(
            ct => runner.RunAsync<string>("prompt", ct),
            fallback: () => "fallback");

        result.Should().Be("retry-result");
        runner.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_FailsBothAttempts_FallsBackWithoutThrowing()
    {
        var runner = new FakeAgentRunner()
            .EnqueueFailure(new InvalidOperationException("schema invalid"))
            .EnqueueFailure(new InvalidOperationException("still invalid"));

        var result = await StructuredOutputValidator.ExecuteAsync(
            ct => runner.RunAsync<string>("prompt", ct),
            fallback: () => "safe-fallback");

        result.Should().Be("safe-fallback");
        runner.CallCount.Should().Be(2);
    }
}
