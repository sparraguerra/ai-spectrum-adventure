namespace AI.SpectrumAdventure.Application.Worlds;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;

public sealed record KnownLocationTravelResult(bool Success, string Narrative);

public sealed class TravelToKnownLocationUseCase(IWorldRepository worldRepository)
{
    public async Task<KnownLocationTravelResult> ExecuteAsync(Game game, string direction, CancellationToken cancellationToken = default)
    {
        if (game.WorldId is not { } worldId || !Enum.TryParse<ConnectionDirection>(direction, true, out var parsedDirection))
            return new(false, "There is no path in that direction.");

        var world = await worldRepository.FindAsync(worldId, cancellationToken);
        var connection = world?.FindConnection(game.Player.CurrentLocationId, parsedDirection);
        if (connection is null || connection.Visibility != ConnectionVisibility.Discovered || connection.Availability != ConnectionAvailability.Available)
            return new(false, "There is no passable known path in that direction.");

        var destination = world!.GetLocation(connection.DestinationLocationId);
        game.AddGeneratedLocation(destination, connection);
        var now = DateTimeOffset.UtcNow;
        game.Apply(new PlayerMovedEvent(Guid.NewGuid(), now, connection.SourceLocationId, destination.Id));
        game.Apply(new LocationDiscoveredEvent(Guid.NewGuid(), now, destination.Id));
        game.PlayerKnowledge.DiscoverRegion(destination.RegionId, now);
        game.PlayerKnowledge.DiscoverLocation(destination.Id, now);
        game.PlayerKnowledge.DiscoverConnection(connection.Id, now);
        await worldRepository.SaveWorldAndGameAsync(world, game, cancellationToken);
        return new(true, destination.BaseDescription);
    }
}