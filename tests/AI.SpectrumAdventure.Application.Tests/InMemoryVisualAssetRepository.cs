namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Games;

/// <summary>In-memory IVisualAssetRepository test double.</summary>
public sealed class InMemoryVisualAssetRepository : IVisualAssetRepository
{
    private readonly Dictionary<string, VisualAsset> _assets = [];

    public Task<VisualAsset?> FindAsync(string sceneStateKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(_assets.GetValueOrDefault(sceneStateKey));

    public Task SaveAsync(VisualAsset asset, CancellationToken cancellationToken = default)
    {
        _assets[asset.SceneStateKey] = asset;
        return Task.CompletedTask;
    }
}
