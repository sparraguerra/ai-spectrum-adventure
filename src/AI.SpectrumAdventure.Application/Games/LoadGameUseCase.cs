namespace AI.SpectrumAdventure.Application.Games;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;

public sealed class LoadGameUseCase(IGameRepository repository)
{
    public Task<Game?> ExecuteAsync(GameId id, CancellationToken cancellationToken = default) =>
        repository.FindAsync(id, cancellationToken);
}
