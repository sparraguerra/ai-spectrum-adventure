namespace AI.SpectrumAdventure.Infrastructure.Speech;

public interface ISpeechSynthesisClient
{
    Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default);
}
