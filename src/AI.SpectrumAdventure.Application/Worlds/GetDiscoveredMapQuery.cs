namespace AI.SpectrumAdventure.Application.Worlds;

using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;

public sealed record DiscoveredMapLocation(string Id, string Name, string RegionId, LocationState State);
public sealed record DiscoveredMapConnection(string Id, string SourceLocationId, string DestinationLocationId, ConnectionDirection Direction, ConnectionAvailability Availability);
public sealed record DiscoveredMap(IReadOnlyCollection<string> RegionIds, IReadOnlyCollection<DiscoveredMapLocation> Locations, IReadOnlyCollection<DiscoveredMapConnection> Connections);

public static class GetDiscoveredMapQuery
{
    public static DiscoveredMap Execute(Game game, World world)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(world);

        var knownLocations = game.PlayerKnowledge.Locations.Keys.ToHashSet(StringComparer.Ordinal);
        var locations = world.Locations.Where(location => knownLocations.Contains(location.Id.Value))
            .OrderBy(location => location.Id.Value, StringComparer.Ordinal)
            .Select(location => new DiscoveredMapLocation(location.Id.Value, location.StructuralNameKey.Replace('-', ' '), location.RegionId.Value, location.State))
            .ToArray();
        var connections = world.Connections.Where(connection => game.PlayerKnowledge.Connections.ContainsKey(connection.Id.Value) && knownLocations.Contains(connection.SourceLocationId.Value) && knownLocations.Contains(connection.DestinationLocationId.Value))
            .OrderBy(connection => connection.Id.Value, StringComparer.Ordinal)
            .Select(connection => new DiscoveredMapConnection(connection.Id.Value, connection.SourceLocationId.Value, connection.DestinationLocationId.Value, connection.Direction, connection.Availability))
            .ToArray();
        return new DiscoveredMap(game.PlayerKnowledge.Regions.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray(), locations, connections);
    }
}