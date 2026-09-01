namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;

/// <summary>A trivial in-memory IGameRepository test double used across Application-layer tests (research.md Decision 6 style).</summary>
public sealed class InMemoryGameRepository : IGameRepository
{
    private readonly Dictionary<GameId, Game> _games = [];

    public Task<Game?> FindAsync(GameId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_games.GetValueOrDefault(id));

    public Task SaveAsync(Game game, CancellationToken cancellationToken = default)
    {
        _games[game.Id] = game;
        return Task.CompletedTask;
    }
}
