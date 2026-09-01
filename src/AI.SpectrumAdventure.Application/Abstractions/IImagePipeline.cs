namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Contracts;

/// <summary>Enqueues a scene for asynchronous, non-blocking image generation (FR-024/FR-025). Enqueuing MUST
/// return immediately — the actual generation happens in a background worker (research.md Decision 9).</summary>
public interface IImagePipeline
{
    void Enqueue(ImageGenerationRequest request);
}
