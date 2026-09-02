namespace AI.SpectrumAdventure.Application.Worlds;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Npcs;
using AI.SpectrumAdventure.Domain.Worlds;

/// <summary>Applies authoritative NPC placement through validated world events.</summary>
public static class NpcWorldStateService
{
    public static LocationId GetLocation(World world, NpcId npcId) =>
        world.NpcLocations.TryGetValue(npcId, out var locationId)
            ? locationId
            : throw new KeyNotFoundException($"NPC '{npcId}' has no canonical world location.");

    public static void Place(World world, Npc npc, LocationId locationId, string cause, DateTimeOffset occurredAt)
    {
        using var activity = WorldTelemetry.Start("npc.placement");
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(npc);
        ArgumentException.ThrowIfNullOrWhiteSpace(cause);
        world.GetLocation(locationId);

        if (world.NpcLocations.TryGetValue(npc.Id, out var existingLocation) && npc.WorldLocationId is not null && existingLocation != npc.WorldLocationId)
        {
            throw new InvalidOperationException($"NPC '{npc.Id}' location is inconsistent with canonical world state.");
        }

        if (world.NpcLocations.TryGetValue(npc.Id, out existingLocation) && existingLocation == locationId)
        {
            npc.Relocate(locationId);
            activity?.SetTag("world.npc_relocated", false);
            return;
        }

        world.Apply(new NpcMovedWorldEvent(WorldEventId.New(), world.Events.Count + 1, occurredAt, cause, npc.Id, locationId));
        npc.Relocate(locationId);
        activity?.SetTag("world.npc_relocated", true);
    }
}