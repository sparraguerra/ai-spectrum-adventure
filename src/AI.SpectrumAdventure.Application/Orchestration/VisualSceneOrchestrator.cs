namespace AI.SpectrumAdventure.Application.Orchestration;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;

/// <summary>
/// Attaches a retro visual to an ActionResult when the scene materially changed (FR-023/FR-024). Reuses a
/// cached VisualAsset when the sceneStateKey is unchanged (Phase 14 cache, constitution Principle X); otherwise
/// enqueues asynchronous generation and returns immediately with VisualAssetUrl left null/pending — the
/// narrative response is never delayed waiting for an image (FR-025, constitution Principle XXII).
/// </summary>
public static class VisualSceneOrchestrator
{
    public static async Task<ActionResult> AttachVisualAsync(
        Game game,
        ActionResult result,
        IVisualArtDirector visualArtDirector,
        IImagePipeline imagePipeline,
        IVisualAssetRepository visualAssetRepository,
        bool ensureCurrentScene = false,
        CancellationToken cancellationToken = default)
    {
        if (!result.SceneChanged && !ensureCurrentScene)
        {
            return result;
        }

        var location = game.CurrentLocation;
        var visibleObjects = location.ObjectIds
            .Select(game.GetItem)
            .Where(item => item.LocationId == location.Id && item.IsRevealed(game.WorldFlagKeys))
            .ToList();
        var sceneStateKey = SceneStateKeyBuilder.Build(location.Id.Value, game.WorldFlagKeys, visibleObjects.Select(item => item.Id.Value));

        var cached = await visualAssetRepository.FindAsync(sceneStateKey, cancellationToken);
        if (cached is { Status: VisualAssetStatus.Ready })
        {
            return result with { VisualAssetUrl = cached.BlobUri };
        }

        var visibleObjectNames = visibleObjects
            .Select(item => item.Name)
            .ToList();

        var context = new VisualContext(location.Id.Value, location.Name, visibleObjectNames, Mood: "atmospheric", sceneStateKey);
        var spec = await visualArtDirector.DescribeSceneAsync(context, cancellationToken);

        imagePipeline.Enqueue(new ImageGenerationRequest(game.Id.Value, spec));

        return result;
    }
}
