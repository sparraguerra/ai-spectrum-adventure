namespace AI.SpectrumAdventure.Application.Worlds;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Worlds;

public sealed record WorldLocationProjection(
    LocationId Id,
    RegionId RegionId,
    string Name,
    string Description,
    LocationState State,
    int SceneVersion,
    IReadOnlyCollection<WorldConnectionProjection> Exits);

public sealed record WorldConnectionProjection(
    ConnectionId Id,
    ConnectionDirection Direction,
    LocationId DestinationLocationId,
    ConnectionVisibility Visibility,
    ConnectionAvailability Availability);

public sealed class GetWorldLocationQuery(IWorldRepository worldRepository)
{
    public async Task<WorldLocationProjection?> ExecuteAsync(WorldId worldId, LocationId locationId, CancellationToken cancellationToken = default)
    {
        var world = await worldRepository.FindAsync(worldId, cancellationToken);
        if (world is null || !world.Locations.Any(location => location.Id == locationId)) return null;

        var location = world.GetLocation(locationId);
        var exits = world.Connections.Where(connection => connection.SourceLocationId == locationId)
            .Select(connection => new WorldConnectionProjection(connection.Id, connection.Direction, connection.DestinationLocationId, connection.Visibility, connection.Availability))
            .ToArray();
        return new(location.Id, location.RegionId, location.StructuralNameKey, location.BaseDescription, location.State, location.SceneVersion, exits);
    }
}