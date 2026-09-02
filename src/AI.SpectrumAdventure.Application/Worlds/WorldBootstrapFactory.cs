namespace AI.SpectrumAdventure.Application.Worlds;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;

public static class WorldBootstrapFactory
{
    public static World Create(Game game, WorldId worldId)
    {
        var regionId = new RegionId($"{game.AdventureId}-initial");
        var region = new Region(regionId, RegionType.Wilderness, [TerrainKind.Forest], new RegionExpansionPolicy(100, new HashSet<ConnectionDirection>(Enum.GetValues<ConnectionDirection>())), game.Locations.Select(location => location.Id).ToHashSet());
        var locations = game.Locations.Select(location => new WorldLocation(location.Id, regionId, LocationType.Landmark, location.Id.Value, location.BaseDescription, new EnvironmentalProperties(TerrainKind.Forest, "temperate", "daylight"))).ToArray();
        var connections = game.Locations.SelectMany(location => location.Exits.Select(exit => new WorldConnection(
            new ConnectionId($"{location.Id.Value}:{exit.Direction}"), location.Id, exit.DestinationId, ParseDirection(exit.Direction), ConnectionVisibility.Discovered))).ToArray();
        var world = new World(worldId, new WorldSeed($"{game.AdventureId}:{game.Id}"), new GenerationVersion("1"), [region], locations, connections);
        foreach (var npc in game.Npcs)
        {
            if (npc.WorldLocationId is not { } locationId) continue;
            world.Apply(new NpcMovedWorldEvent(WorldEventId.New(), world.Events.Count + 1, game.CreatedAt, WorldEvent.FormatCause(WorldEventCause.WorldRule, "initial-npc-placement"), npc.Id, locationId));
        }

        return world;
    }

    private static ConnectionDirection ParseDirection(string direction) => Enum.TryParse<ConnectionDirection>(direction, true, out var result)
        ? result
        : throw new InvalidOperationException($"Adventure exit direction '{direction}' is not a world direction.");
}