namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;

internal static class E2ETestSupport
{
    public static Game CreateStartedGame()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        game.Apply(new LocationDiscoveredEvent(Guid.NewGuid(), game.CreatedAt, AdventureWorldFactory.ForestEntrance));
        return game;
    }

    public static ParsedIntent Intent(IntentAction action, string? target = null, IReadOnlyDictionary<string, string>? parameters = null) =>
        new(action, target, parameters ?? new Dictionary<string, string>(), 1, target ?? action.ToString());

    public static void GrantTowerClue(Game game) =>
        game.Apply(new NpcRelationshipChangedEvent(
            Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.Hermit, WorldFlags.NpcGaveClue,
            "Tell me about the tower.", "Only a patient hand may open it."));
}

internal sealed class E2EInMemoryGameRepository : IGameRepository
{
    private readonly Dictionary<GameId, Game> _games = [];

    public Task<Game?> FindAsync(GameId id, CancellationToken cancellationToken = default) => Task.FromResult(_games.GetValueOrDefault(id));

    public Task SaveAsync(Game game, CancellationToken cancellationToken = default)
    {
        _games[game.Id] = game;
        return Task.CompletedTask;
    }
}

internal sealed class StaticVisualArtDirector : IVisualArtDirector
{
    public Task<VisualSceneSpec> DescribeSceneAsync(VisualContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(new VisualSceneSpec(context.LocationId, context.SceneStateKey, null, [], context.Mood, "zx-spectrum-8bit"));
}

internal sealed class RecordingImagePipeline : IImagePipeline
{
    public List<ImageGenerationRequest> Requests { get; } = [];

    public void Enqueue(ImageGenerationRequest request) => Requests.Add(request);
}

internal sealed class E2EVisualAssetRepository : IVisualAssetRepository
{
    public Task<VisualAsset?> FindAsync(string sceneStateKey, CancellationToken cancellationToken = default) => Task.FromResult<VisualAsset?>(null);

    public Task SaveAsync(VisualAsset asset, CancellationToken cancellationToken = default) => Task.CompletedTask;
}