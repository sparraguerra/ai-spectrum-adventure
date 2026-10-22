namespace AI.SpectrumAdventure.Application.Abstractions;

public interface ISceneNarrationService
{
    Task<SceneNarrationAudio> SynthesizeAsync(string text, CancellationToken cancellationToken = default);
}

public sealed record SceneNarrationAudio(byte[] AudioBytes, string ContentType);

public sealed class SceneNarrationException(string message, Exception? innerException = null)
    : Exception(message, innerException);
