namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;

/// <summary>Persistence boundary for the authoritative shared World aggregate.</summary>
public interface IWorldRepository
{
    Task CreateInitialWorldAsync(World world, Game game, CancellationToken cancellationToken = default);
    Task<World?> FindAsync(WorldId id, CancellationToken cancellationToken = default);
    Task SaveAsync(World world, CancellationToken cancellationToken = default);
    Task SaveWorldAndGameAsync(World world, Game game, CancellationToken cancellationToken = default);
    Task<World> PersistExpansionAsync(World world, WorldExpandedEvent expansion, CancellationToken cancellationToken = default);
    Task<World> MaterializeLegacyWorldAsync(Game game, CancellationToken cancellationToken = default);
}