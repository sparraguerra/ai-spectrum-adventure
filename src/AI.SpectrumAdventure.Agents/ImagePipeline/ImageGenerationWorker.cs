namespace AI.SpectrumAdventure.Agents.ImagePipeline;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Games;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Drains ImageGenerationQueue in the background: generate -> retro-process -> upload -> record VisualAsset.
/// A failure at any stage marks the VisualAsset Failed and is traced (constitution Principle IX) but never
/// propagates to the player — gameplay already returned its narrative response before this worker even starts
/// (FR-024/FR-025, Edge Case: Visual Failure).
/// </summary>
public sealed class ImageGenerationWorker(
    ImageGenerationQueue queue,
    IServiceScopeFactory scopeFactory,
    ISceneUpdateNotifier sceneUpdateNotifier,
    ILogger<ImageGenerationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in queue.Reader.ReadAllAsync(stoppingToken))
        {
            var spec = request.Scene;
            using var activity = AgentTelemetry.ActivitySource.StartActivity("image-pipeline.generate");
            activity?.SetTag("scene.state_key", spec.SceneStateKey);

            using var scope = scopeFactory.CreateScope();
            var generator = scope.ServiceProvider.GetRequiredService<IImageGenerator>();
            var processor = scope.ServiceProvider.GetRequiredService<IRetroImageProcessor>();
            var blobStore = scope.ServiceProvider.GetRequiredService<IVisualAssetBlobStore>();
            var repository = scope.ServiceProvider.GetRequiredService<IVisualAssetRepository>();

            var asset = await repository.FindAsync(spec.SceneStateKey, stoppingToken) ?? new VisualAsset(spec.SceneStateKey);
            activity?.SetTag("image.cache_hit", asset.Status == VisualAssetStatus.Ready);
            activity?.SetTag("image.generation_attempted", true);

            try
            {
                var rawImage = await generator.GenerateAsync(spec, stoppingToken);
                var processedImage = processor.Process(rawImage);
                var blobUri = await blobStore.UploadAsync(spec.SceneStateKey, processedImage, stoppingToken);

                asset.MarkReady(blobUri, DateTimeOffset.UtcNow);
                activity?.SetTag("image.success", true);
                activity?.SetTag("image.status", asset.Status.ToString());
            }
            catch (Exception ex)
            {
                asset.MarkFailed();
                activity?.SetTag("image.success", false);
                activity?.SetTag("image.status", asset.Status.ToString());
                activity?.SetTag("image.error_type", ex.GetType().Name);
                logger.LogWarning(ex, "Image generation failed for scene {SceneStateKey}; gameplay continues unaffected.", spec.SceneStateKey);
            }

            await repository.SaveAsync(asset, stoppingToken);
            await sceneUpdateNotifier.NotifyAsync(
                new SceneUpdate(request.GameId, spec.SceneStateKey, asset.BlobUri, asset.Status),
                stoppingToken);
        }
    }
}
