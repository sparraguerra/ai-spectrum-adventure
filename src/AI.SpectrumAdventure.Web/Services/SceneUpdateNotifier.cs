namespace AI.SpectrumAdventure.Web.Services;

using System.Collections.Concurrent;
using AI.SpectrumAdventure.Application.Abstractions;

public sealed class SceneUpdateNotifier : ISceneUpdateNotifier
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Func<SceneUpdate, Task>>> _subscriptions = [];

    public IDisposable Subscribe(Guid gameId, Func<SceneUpdate, Task> handler)
    {
        var subscriptionId = Guid.NewGuid();
        var handlers = _subscriptions.GetOrAdd(gameId, _ => []);
        handlers[subscriptionId] = handler;

        return new Subscription(() =>
        {
            if (_subscriptions.TryGetValue(gameId, out var currentHandlers))
            {
                currentHandlers.TryRemove(subscriptionId, out _);
                if (currentHandlers.IsEmpty)
                {
                    _subscriptions.TryRemove(new KeyValuePair<Guid, ConcurrentDictionary<Guid, Func<SceneUpdate, Task>>>(gameId, currentHandlers));
                }
            }
        });
    }

    public async Task NotifyAsync(SceneUpdate update, CancellationToken cancellationToken = default)
    {
        if (!_subscriptions.TryGetValue(update.GameId, out var handlers))
        {
            return;
        }

        foreach (var handler in handlers.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await handler(update);
            }
            catch
            {
                // A disposed or failed circuit must not interrupt image processing for other players.
            }
        }
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        public void Dispose() => unsubscribe();
    }
}