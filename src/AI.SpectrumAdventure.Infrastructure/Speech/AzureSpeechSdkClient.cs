namespace AI.SpectrumAdventure.Infrastructure.Speech;

using AI.SpectrumAdventure.Application.Abstractions;
using Azure.Identity;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Options;

public sealed class AzureSpeechSdkClient(
    IOptions<AzureSpeechOptions> speechOptions,
    NarrationSsmlBuilder ssmlBuilder) : ISpeechSynthesisClient
{
    private readonly AzureSpeechOptions options = speechOptions.Value;

    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var speechConfig = CreateSpeechConfig();
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio24Khz160KBitRateMonoMp3);

            using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig: null);
            using var cancellationRegistration = cancellationToken.Register(
                static state => _ = ((SpeechSynthesizer)state!).StopSpeakingAsync(),
                synthesizer);
            using var result = await synthesizer.SpeakSsmlAsync(ssmlBuilder.Build(text)).WaitAsync(cancellationToken);

            if (result.Reason != ResultReason.SynthesizingAudioCompleted)
            {
                var details = SpeechSynthesisCancellationDetails.FromResult(result);
                throw new SceneNarrationException(
                    $"Azure Speech synthesis did not complete ({details.Reason}: {details.ErrorCode}).");
            }

            if (result.AudioData.Length == 0)
            {
                throw new SceneNarrationException("Azure Speech returned empty narration audio.");
            }

            return result.AudioData;
        }
        catch (SceneNarrationException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new SceneNarrationException(
                $"Azure Speech request failed ({exception.GetType().Name}).",
                exception);
        }
    }

    private SpeechConfig CreateSpeechConfig()
    {
        if (!string.IsNullOrWhiteSpace(options.Key))
        {
            if (!string.IsNullOrWhiteSpace(options.Endpoint))
            {
                return SpeechConfig.FromEndpoint(new Uri(options.Endpoint), options.Key);
            }

            if (string.IsNullOrWhiteSpace(options.Region))
            {
                throw new SceneNarrationException("Azure Speech region must be configured when using a resource key.");
            }

            return SpeechConfig.FromSubscription(options.Key, options.Region);
        }

        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return SpeechConfig.FromEndpoint(new Uri(options.Endpoint), new DefaultAzureCredential());
        }

        throw new SceneNarrationException("Configure Azure Speech credentials before requesting narration.");
    }
}
