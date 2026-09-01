namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Agents.ImagePipeline;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Web.Services;
using FluentAssertions;

public sealed class AsyncUiUpdateTests
{
    [Fact]
    public async Task PlayerCanProcessNextAction_WhilePreviousSceneGenerationIsPending()
    {
        var game = AdventureWorldFactory.CreateNewGame(new GameId(Guid.NewGuid()), DateTimeOffset.UtcNow);
        var moved = AdventureOrchestrator.ProcessAction(game, new ParsedIntent(IntentAction.Go, "north", new Dictionary<string, string>(), 1, "go north"));
        var queue = new ImageGenerationQueue();

        await VisualSceneOrchestrator.AttachVisualAsync(
            game,
            moved,
            new StubVisualArtDirector(),
            queue,
            new EmptyVisualAssetRepository());

        var nextAction = AdventureOrchestrator.ProcessAction(game, new ParsedIntent(IntentAction.Look, null, new Dictionary<string, string>(), 1, "look"));

        nextAction.Success.Should().BeTrue();
        queue.Reader.TryRead(out var pendingRequest).Should().BeTrue();
        pendingRequest.Should().NotBeNull();
        pendingRequest!.GameId.Should().Be(game.Id.Value);
    }

    [Fact]
    public async Task TakingVisibleItem_EnqueuesANewSceneWithoutThatItem()
    {
        var game = AdventureWorldFactory.CreateNewGame(new GameId(Guid.NewGuid()), DateTimeOffset.UtcNow);
        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));
        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.DarkForest, AdventureWorldFactory.OldBridge));
        var queue = new ImageGenerationQueue();

        await VisualSceneOrchestrator.AttachVisualAsync(
            game,
            new ActionResult(game.Id.Value, true, null, "You arrive at the bridge.", SceneChanged: true, AdventureWorldFactory.OldBridge.Value, [], null),
            new StubVisualArtDirector(),
            queue,
            new EmptyVisualAssetRepository());
        queue.Reader.TryRead(out var beforePickup).Should().BeTrue();

        game.Apply(new ItemTakenEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.BridgeKey, AdventureWorldFactory.OldBridge));

        await VisualSceneOrchestrator.AttachVisualAsync(
            game,
            new ActionResult(game.Id.Value, true, null, "You take the Rusted Bridge Key.", SceneChanged: true, AdventureWorldFactory.OldBridge.Value, [AdventureWorldFactory.BridgeKey.Value], null),
            new StubVisualArtDirector(),
            queue,
            new EmptyVisualAssetRepository());
        queue.Reader.TryRead(out var afterPickup).Should().BeTrue();

        beforePickup!.Scene.SceneStateKey.Should().Contain("objects:bridge-key");
        beforePickup.Scene.Objects.Should().Contain("Rusted Bridge Key");
        afterPickup!.Scene.SceneStateKey.Should().Contain("objects:");
        afterPickup.Scene.SceneStateKey.Should().NotBe(beforePickup.Scene.SceneStateKey);
        afterPickup.Scene.Objects.Should().NotContain("Rusted Bridge Key");
    }

    [Fact]
    public async Task ReadySceneNotification_IsDeliveredOnlyToTheMatchingGame()
    {
        var notifier = new SceneUpdateNotifier();
        var expectedGameId = Guid.NewGuid();
        var notification = new TaskCompletionSource<SceneUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var subscription = notifier.Subscribe(expectedGameId, update =>
        {
            notification.TrySetResult(update);
            return Task.CompletedTask;
        });

        await notifier.NotifyAsync(new SceneUpdate(expectedGameId, "dark-forest", "https://assets.example/dark-forest.png", VisualAssetStatus.Ready));
        var received = await notification.Task.WaitAsync(TimeSpan.FromSeconds(1));

        received.GameId.Should().Be(expectedGameId);
        received.VisualAssetUrl.Should().Be("https://assets.example/dark-forest.png");
    }

    private sealed class StubVisualArtDirector : IVisualArtDirector
    {
        public Task<VisualSceneSpec> DescribeSceneAsync(VisualContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new VisualSceneSpec(context.LocationId, context.SceneStateKey, null, context.VisibleObjectNames, context.Mood, "zx-spectrum-8bit"));
    }

    private sealed class EmptyVisualAssetRepository : IVisualAssetRepository
    {
        public Task<VisualAsset?> FindAsync(string sceneStateKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<VisualAsset?>(null);

        public Task SaveAsync(VisualAsset asset, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}