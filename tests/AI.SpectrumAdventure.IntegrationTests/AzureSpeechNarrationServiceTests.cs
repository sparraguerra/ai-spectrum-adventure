namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Infrastructure.Speech;
using FluentAssertions;
using Microsoft.Extensions.Options;

public sealed class AzureSpeechNarrationServiceTests
{
    [Fact]
    public async Task SynthesizeAsync_ReturnsAudioFromSpeechClient()
    {
        var expectedAudio = new byte[] { 1, 2, 3 };
        var client = new FakeSpeechSynthesisClient(expectedAudio);
        var service = CreateService(client);

        var result = await service.SynthesizeAsync("The forest is silent.");

        result.AudioBytes.Should().Equal(expectedAudio);
        result.ContentType.Should().Be("audio/mpeg");
        client.CallCount.Should().Be(1);
        client.LastText.Should().Be("The forest is silent.");
    }

    [Fact]
    public async Task SynthesizeAsync_ReusesCachedAudioForRepeatedText()
    {
        var expectedAudio = new byte[] { 4, 5, 6 };
        var client = new FakeSpeechSynthesisClient(expectedAudio);
        var service = CreateService(client);

        var first = await service.SynthesizeAsync("A door creaks open.");
        var second = await service.SynthesizeAsync("A door creaks open.");

        second.AudioBytes.Should().Equal(first.AudioBytes);
        client.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task SynthesizeAsync_DoesNotReuseAudioWhenProsodyChanges()
    {
        var expectedAudio = new byte[] { 7, 8, 9 };
        var client = new FakeSpeechSynthesisClient(expectedAudio);
        var options = new AzureSpeechOptions();
        var service = new AzureSpeechNarrationService(client, Options.Create(options));

        await service.SynthesizeAsync("A door creaks open.");
        options.Rate = "-30%";
        await service.SynthesizeAsync("A door creaks open.");

        client.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task SynthesizeAsync_DoesNotCallSpeechClientWhenAlreadyCancelled()
    {
        var client = new FakeSpeechSynthesisClient([1]);
        var service = CreateService(client);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var act = () => service.SynthesizeAsync("This narration is no longer current.", cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        client.CallCount.Should().Be(0);
    }

    private static AzureSpeechNarrationService CreateService(FakeSpeechSynthesisClient client) =>
        new(client, Options.Create(new AzureSpeechOptions()));

    private sealed class FakeSpeechSynthesisClient(byte[] audio) : ISpeechSynthesisClient
    {
        public int CallCount { get; private set; }

        public string? LastText { get; private set; }

        public Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            LastText = text;
            return Task.FromResult(audio);
        }
    }
}
