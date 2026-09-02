namespace AI.SpectrumAdventure.Application.Orchestration;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;

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
        World? world = null,
        IWorldEnrichmentRepository? enrichmentRepository = null,
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
        var worldLocation = world?.Locations.SingleOrDefault(candidate => candidate.Id.Value == location.Id.Value);
        var sceneStateKey = worldLocation is null
            ? SceneStateKeyBuilder.Build(location.Id.Value, game.WorldFlagKeys, visibleObjects.Select(item => item.Id.Value))
            : SceneStateKeyBuilder.Build(world!.Id.Value, worldLocation.Id.Value, worldLocation.SceneVersion, game.WorldFlagKeys, visibleObjects.Select(item => item.Id.Value));

        var cached = await visualAssetRepository.FindAsync(sceneStateKey, cancellationToken);
        if (cached is { Status: VisualAssetStatus.Ready })
        {
            return result with { VisualAssetUrl = cached.BlobUri };
        }

        var presentation = worldLocation is not null && enrichmentRepository is not null
            ? await enrichmentRepository.FindAsync(world!.Id.Value, worldLocation.Id.Value, worldLocation.SceneVersion, cancellationToken)
            : null;
        var visibleObjectNames = visibleObjects
            .Select(item => item.Name)
            .Concat(presentation?.VisualCharacteristics ?? [])
            .ToList();

        var context = new VisualContext(location.Id.Value, presentation?.DisplayName ?? location.Name, visibleObjectNames,
            presentation?.Atmosphere ?? "atmospheric", sceneStateKey, presentation?.VisualCharacteristics);
        var spec = await visualArtDirector.DescribeSceneAsync(context, cancellationToken);

        imagePipeline.Enqueue(new ImageGenerationRequest(game.Id.Value, spec));

        return result;
    }
}
