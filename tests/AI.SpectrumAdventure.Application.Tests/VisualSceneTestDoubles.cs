namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;

/// <summary>Records every Enqueue call without doing any real work — used to assert non-blocking enqueue behavior.</summary>
public sealed class RecordingImagePipeline : IImagePipeline
{
    public List<ImageGenerationRequest> EnqueuedRequests { get; } = [];

    public void Enqueue(ImageGenerationRequest request) => EnqueuedRequests.Add(request);
}

/// <summary>A fake IVisualArtDirector test double.</summary>
public sealed class FakeVisualArtDirector(VisualSceneSpec response) : IVisualArtDirector
{
    public Task<VisualSceneSpec> DescribeSceneAsync(VisualContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(response with { SceneStateKey = context.SceneStateKey });
}
