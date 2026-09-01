namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;

/// <summary>Persistence abstraction for the Game aggregate; implemented in Infrastructure (Phase 7), never referenced by Domain.</summary>
public interface IGameRepository
{
    Task<Game?> FindAsync(GameId id, CancellationToken cancellationToken = default);

    Task SaveAsync(Game game, CancellationToken cancellationToken = default);
}
