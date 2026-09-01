namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Domain.Games;

/// <summary>Persists VisualAsset read models keyed by sceneStateKey (Phase 14 cache backing store).</summary>
public interface IVisualAssetRepository
{
    Task<VisualAsset?> FindAsync(string sceneStateKey, CancellationToken cancellationToken = default);

    Task SaveAsync(VisualAsset asset, CancellationToken cancellationToken = default);
}
