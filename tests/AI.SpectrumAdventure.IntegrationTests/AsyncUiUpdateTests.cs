namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Agents.ImagePipeline;
using AI.SpectrumAdventure.Agents.Intent;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Authoring;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Web.Services;
using FluentAssertions;

public sealed class AsyncUiUpdateTests
{
    [Fact]
    public async Task ReadySceneFromAnotherWorldOfSameAdventure_IsReusedWithoutEnqueue()
    {
        var adventureVersionId = AdventureVersionId.New();
        var createdAt = DateTimeOffset.UtcNow;
        var firstGame = AdventureWorldFactory.CreateNewGame(new GameId(Guid.NewGuid()), createdAt, AdventureWorldFactory.DefaultAdventureId, adventureVersionId);
        var secondGame = AdventureWorldFactory.CreateNewGame(new GameId(Guid.NewGuid()), createdAt, AdventureWorldFactory.DefaultAdventureId, adventureVersionId);
        var firstWorld = WorldBootstrapFactory.Create(firstGame, new WorldId(Guid.NewGuid()));
        var secondWorld = WorldBootstrapFactory.Create(secondGame, new WorldId(Guid.NewGuid()));
        var visibleObjectIds = firstGame.CurrentLocation.ObjectIds
            .Select(firstGame.GetItem)
            .Where(item => item.LocationId == firstGame.CurrentLocation.Id && item.IsRevealed(firstGame.WorldFlagKeys))
            .Select(item => item.Id.Value)
            .ToList();
        var sceneStateKey = AI.SpectrumAdventure.Application.Games.SceneStateKeyBuilder.Build(
            firstGame.AdventureId,
            firstGame.AdventureVersionId?.Value,
            firstWorld.GetLocation(firstGame.CurrentLocation.Id).Id.Value,
            firstWorld.GetLocation(firstGame.CurrentLocation.Id).SceneVersion,
            firstGame.WorldFlagKeys,
            visibleObjectIds);
        var cachedImageUrl = "https://assets.example/forest-entrance.png";
        var repository = new ReadyVisualAssetRepository(sceneStateKey, cachedImageUrl);
        var queue = new ImageGenerationQueue();
        var result = new ActionResult(secondGame.Id.Value, true, null, "The scene is unchanged.", true, secondGame.CurrentLocation.Id.Value, [], null);

        var attachedResult = await VisualSceneOrchestrator.AttachVisualAsync(
            secondGame,
            result,
            new StubVisualArtDirector(),
            queue,
            repository,
            ensureCurrentScene: true,
            world: secondWorld);

        repository.LastQueriedKey.Should().Be(sceneStateKey);
        attachedResult.VisualAssetUrl.Should().Be(cachedImageUrl);
        queue.Reader.TryRead(out _).Should().BeFalse();
        firstWorld.Id.Should().NotBe(secondWorld.Id);
    }

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
    public async Task DroppingInventoryItem_RefreshesNarrativeAndEnqueuesSceneWithDroppedItem()
    {
        var game = AdventureWorldFactory.CreateNewGame(new GameId(Guid.NewGuid()), DateTimeOffset.UtcNow);
        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForestEntrance, AdventureWorldFactory.DarkForest));
        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.DarkForest, AdventureWorldFactory.OldBridge));
        AdventureOrchestrator.ProcessAction(game, new ParsedIntent(IntentAction.Take, AdventureWorldFactory.BridgeKey.Value, new Dictionary<string, string>(), 1, "take bridge key"));
        var dropIntent = CommandPatternInterpreter.TryInterpret("leave the bridge key");

        Enum.TryParse<IntentAction>("Drop", out var dropAction).Should().BeTrue();
        dropIntent.Should().NotBeNull();
        dropIntent!.Action.Should().Be(dropAction);
        var dropped = AdventureOrchestrator.ProcessAction(game, dropIntent);

        dropped.Success.Should().BeTrue();
        dropped.SceneChanged.Should().BeTrue();
        dropped.Narrative.Should().Contain("Rusted Bridge Key");
        game.Player.Inventory.Contains(AdventureWorldFactory.BridgeKey).Should().BeFalse();
        game.CurrentLocation.ObjectIds.Should().Contain(AdventureWorldFactory.BridgeKey);

        var queue = new ImageGenerationQueue();
        await VisualSceneOrchestrator.AttachVisualAsync(
            game,
            dropped,
            new StubVisualArtDirector(),
            queue,
            new EmptyVisualAssetRepository());

        queue.Reader.TryRead(out var refreshedScene).Should().BeTrue();
        refreshedScene!.Scene.Objects.Should().Contain("Rusted Bridge Key");
        refreshedScene.Scene.SceneStateKey.Should().Contain("objects:bridge-key");
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

    private sealed class ReadyVisualAssetRepository(string expectedSceneStateKey, string blobUri) : IVisualAssetRepository
    {
        public string? LastQueriedKey { get; private set; }

        public Task<VisualAsset?> FindAsync(string sceneStateKey, CancellationToken cancellationToken = default)
        {
            LastQueriedKey = sceneStateKey;
            if (!string.Equals(sceneStateKey, expectedSceneStateKey, StringComparison.Ordinal))
                return Task.FromResult<VisualAsset?>(null);

            var asset = new VisualAsset(sceneStateKey);
            asset.MarkReady(blobUri, DateTimeOffset.UnixEpoch);
            return Task.FromResult<VisualAsset?>(asset);
        }

        public Task SaveAsync(VisualAsset asset, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}