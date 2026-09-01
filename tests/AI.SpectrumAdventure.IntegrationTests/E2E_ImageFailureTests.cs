namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Agents.ImagePipeline;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Web.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class E2E_ImageFailureTests
{
    [Fact]
    public async Task ImageGenerationFailure_DoesNotInterruptGameplay()
    {
        var queue = new ImageGenerationQueue();
        var notifier = new SceneUpdateNotifier();
        var updateReceived = new TaskCompletionSource<SceneUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gameId = Guid.NewGuid();
        using var matchingSubscription = notifier.Subscribe(gameId, update =>
        {
            updateReceived.TrySetResult(update);
            return Task.CompletedTask;
        });

        var services = new ServiceCollection()
            .AddScoped<IImageGenerator, FailingImageGenerator>()
            .AddScoped<IRetroImageProcessor, PassThroughImageProcessor>()
            .AddScoped<IVisualAssetBlobStore, UnusedBlobStore>()
            .AddScoped<IVisualAssetRepository, EphemeralVisualAssetRepository>()
            .BuildServiceProvider();
        var worker = new ImageGenerationWorker(queue, services.GetRequiredService<IServiceScopeFactory>(), notifier, NullLogger<ImageGenerationWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            var game = E2ETestSupport.CreateStartedGame();
            queue.Enqueue(new ImageGenerationRequest(gameId, new VisualSceneSpec("forest-entrance", "forest-entrance", null, [], "still", "zx-spectrum-8bit")));

            var gameplayResult = AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Look));
            var update = await updateReceived.Task.WaitAsync(TimeSpan.FromSeconds(2));

            gameplayResult.Success.Should().BeTrue();
            update.Status.Should().Be(VisualAssetStatus.Failed);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
            await services.DisposeAsync();
        }
    }

    private sealed class FailingImageGenerator : IImageGenerator
    {
        public Task<byte[]> GenerateAsync(VisualSceneSpec spec, CancellationToken cancellationToken = default) =>
            Task.FromException<byte[]>(new InvalidOperationException("Simulated generator failure."));
    }

    private sealed class PassThroughImageProcessor : IRetroImageProcessor
    {
        public byte[] Process(byte[] rawImageBytes) => rawImageBytes;
    }

    private sealed class UnusedBlobStore : IVisualAssetBlobStore
    {
        public Task<string> UploadAsync(string sceneStateKey, byte[] processedImageBytes, CancellationToken cancellationToken = default) =>
            Task.FromResult("https://assets.example/unused.png");
    }

    private sealed class EphemeralVisualAssetRepository : IVisualAssetRepository
    {
        public Task<VisualAsset?> FindAsync(string sceneStateKey, CancellationToken cancellationToken = default) => Task.FromResult<VisualAsset?>(null);

        public Task SaveAsync(VisualAsset asset, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}