namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Infrastructure.Speech;
using FluentAssertions;
using Microsoft.Extensions.Options;

public sealed class SceneNarrationFailureTests
{
    [Fact]
    public async Task SynthesizeAsync_RetriesAfterFailureWithoutCachingFailedAudio()
    {
        var client = new SequencedSpeechSynthesisClient((call, _, _) =>
            call == 1
                ? Task.FromException<byte[]>(new SceneNarrationException("Speech synthesis failed."))
                : Task.FromResult(new byte[] { 4, 5, 6 }));
        using var service = CreateService(client);

        var firstAttempt = () => service.SynthesizeAsync("The forest is silent.");

        await firstAttempt.Should().ThrowAsync<SceneNarrationException>();
        var result = await service.SynthesizeAsync("The forest is silent.");

        result.AudioBytes.Should().Equal([4, 5, 6]);
        client.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task SynthesizeAsync_PropagatesCancellationAndAllowsLaterNarration()
    {
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new SequencedSpeechSynthesisClient(async (call, _, cancellationToken) =>
        {
            if (call == 1)
            {
                requestStarted.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return [7, 8, 9];
        });
        using var service = CreateService(client);
        using var cancellation = new CancellationTokenSource();

        var pendingNarration = service.SynthesizeAsync("An abandoned scene.", cancellation.Token);
        await requestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cancellation.CancelAsync();

        var cancelledAttempt = async () => await pendingNarration;
        await cancelledAttempt.Should().ThrowAsync<OperationCanceledException>();
        var laterNarration = await service.SynthesizeAsync("A new scene.");

        laterNarration.AudioBytes.Should().Equal([7, 8, 9]);
        client.CallCount.Should().Be(2);
    }

    private static AzureSpeechNarrationService CreateService(ISpeechSynthesisClient client) =>
        new(client, Options.Create(new AzureSpeechOptions()));

    private sealed class SequencedSpeechSynthesisClient(
        Func<int, string, CancellationToken, Task<byte[]>> synthesize) : ISpeechSynthesisClient
    {
        public int CallCount { get; private set; }

        public Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default) =>
            synthesize(++CallCount, text, cancellationToken);
    }
}
