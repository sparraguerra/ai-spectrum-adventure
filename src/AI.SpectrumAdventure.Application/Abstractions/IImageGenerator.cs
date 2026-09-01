namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Contracts;

/// <summary>Calls the underlying image generation model from a validated VisualSceneSpec (research.md Decision 5).</summary>
public interface IImageGenerator
{
    Task<byte[]> GenerateAsync(VisualSceneSpec spec, CancellationToken cancellationToken = default);
}
