namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Domain.Games;

public sealed record SceneUpdate(Guid GameId, string SceneStateKey, string? VisualAssetUrl, VisualAssetStatus Status);

/// <summary>Delivers non-authoritative visual completion events to the active UI circuit for a game.</summary>
public interface ISceneUpdateNotifier
{
    IDisposable Subscribe(Guid gameId, Func<SceneUpdate, Task> handler);

    Task NotifyAsync(SceneUpdate update, CancellationToken cancellationToken = default);
}