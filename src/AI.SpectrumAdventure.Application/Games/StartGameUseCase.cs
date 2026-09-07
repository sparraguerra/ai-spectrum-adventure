namespace AI.SpectrumAdventure.Application.Games;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Application.Worlds;
using AI.SpectrumAdventure.Domain.Authoring;

/// <summary>FR-001: begins a new adventure at the Forest Entrance and persists it.</summary>
public sealed class StartGameUseCase
{
    private readonly IGameRepository repository;
    private readonly IAdventureCatalog? adventureCatalog;
    private readonly IWorldRepository? worldRepository;

    public StartGameUseCase(IGameRepository repository, IAdventureCatalog? adventureCatalog = null, IWorldRepository? worldRepository = null)
    {
        this.repository = repository;
        this.adventureCatalog = adventureCatalog;
        this.worldRepository = worldRepository;
    }

    public async Task<Game> ExecuteAsync(string? adventureId = null, AdventureVersionId? adventureVersionId = null, CancellationToken cancellationToken = default)
    {
        var selectedAdventureId = string.IsNullOrWhiteSpace(adventureId) ? AdventureWorldFactory.DefaultAdventureId : adventureId;
        var selection = adventureCatalog is null ? null : await adventureCatalog.GetDefinitionAsync(selectedAdventureId, adventureVersionId, cancellationToken);
        var game = selection is null
            ? AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow, selectedAdventureId)
            : await CreateAndPersistAsync(selection.AdventureId, selection.DefinitionJson, selection.VersionId, cancellationToken);

        if (selection is null)
        {
            await InitializeAndPersistAsync(game, cancellationToken);
        }

        return game;
    }

    public Task<Game> ExecuteFromDefinitionAsync(string adventureId, string definitionJson, AdventureVersionId? adventureVersionId = null, CancellationToken cancellationToken = default) =>
        CreateAndPersistAsync(adventureId, definitionJson, adventureVersionId, cancellationToken);

    private async Task<Game> CreateAndPersistAsync(string adventureId, string definitionJson, AdventureVersionId? adventureVersionId, CancellationToken cancellationToken)
    {
        var game = AdventureWorldFactory.CreateNewGameFromJson(GameId.New(), DateTimeOffset.UtcNow, definitionJson, adventureVersionId);
        await InitializeAndPersistAsync(game, cancellationToken);
        return game;
    }

    private async Task InitializeAndPersistAsync(Game game, CancellationToken cancellationToken)
    {

        game.Apply(new LocationDiscoveredEvent(Guid.NewGuid(), game.CreatedAt, game.Player.CurrentLocationId));

        if (worldRepository is null)
        {
            await repository.SaveAsync(game, cancellationToken);
        }
        else
        {
            var worldId = WorldId.New();
            var world = WorldBootstrapFactory.Create(game, worldId);
            game.BindWorld(worldId);
            var startingLocation = world.GetLocation(game.Player.CurrentLocationId);
            game.PlayerKnowledge.DiscoverRegion(startingLocation.RegionId, game.CreatedAt);
            game.PlayerKnowledge.DiscoverLocation(startingLocation.Id, game.CreatedAt);
            await worldRepository.CreateInitialWorldAsync(world, game, cancellationToken);
        }
    }
}
