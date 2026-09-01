namespace AI.SpectrumAdventure.Application.Games;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;

/// <summary>FR-001: begins a new adventure at the Forest Entrance and persists it.</summary>
public sealed class StartGameUseCase
{
    private readonly IGameRepository repository;
    private readonly IAdventureCatalog? adventureCatalog;

    public StartGameUseCase(IGameRepository repository, IAdventureCatalog? adventureCatalog = null)
    {
        this.repository = repository;
        this.adventureCatalog = adventureCatalog;
    }

    public async Task<Game> ExecuteAsync(string? adventureId = null, CancellationToken cancellationToken = default)
    {
        var selectedAdventureId = string.IsNullOrWhiteSpace(adventureId) ? AdventureWorldFactory.DefaultAdventureId : adventureId;
        var game = adventureCatalog is null
            ? AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow, selectedAdventureId)
            : AdventureWorldFactory.CreateNewGameFromJson(GameId.New(), DateTimeOffset.UtcNow, await adventureCatalog.GetDefinitionJsonAsync(selectedAdventureId, cancellationToken));

        game.Apply(new LocationDiscoveredEvent(Guid.NewGuid(), game.CreatedAt, game.Player.CurrentLocationId));

        await repository.SaveAsync(game, cancellationToken);
        return game;
    }
}
