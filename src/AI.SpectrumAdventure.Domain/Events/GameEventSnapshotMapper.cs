namespace AI.SpectrumAdventure.Domain.Events;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Items;

/// <summary>Converts between typed GameEvent instances and their flat, serializable GameEventSnapshot mirror.</summary>
public static class GameEventSnapshotMapper
{
    public static GameEventSnapshot ToSnapshot(GameEvent gameEvent) => gameEvent switch
    {
        PlayerMovedEvent e => new(e.Id, e.OccurredAt, nameof(GameEventKind.PlayerMoved), new()
        {
            ["from"] = e.FromLocationId.Value,
            ["to"] = e.ToLocationId.Value,
        }),
        ItemTakenEvent e => new(e.Id, e.OccurredAt, nameof(GameEventKind.ItemTaken), new()
        {
            ["item"] = e.ItemId.Value,
            ["location"] = e.LocationId.Value,
        }),
        ItemUsedEvent e => new(e.Id, e.OccurredAt, nameof(GameEventKind.ItemUsed), BuildItemUsedPayload(e)),
        ObjectStateChangedEvent e => new(e.Id, e.OccurredAt, nameof(GameEventKind.ObjectStateChanged), new()
        {
            ["item"] = e.ItemId.Value,
            ["state"] = ((int)e.NewState).ToString(),
        }),
        DoorOpenedEvent e => new(e.Id, e.OccurredAt, nameof(GameEventKind.DoorOpened), new()
        {
            ["location"] = e.LocationId.Value,
            ["direction"] = e.ExitDirection,
        }),
        NpcRelationshipChangedEvent e => new(e.Id, e.OccurredAt, nameof(GameEventKind.NpcRelationshipChanged), BuildNpcPayload(e)),
        PuzzleSolvedEvent e => new(e.Id, e.OccurredAt, nameof(GameEventKind.PuzzleSolved), new()
        {
            ["puzzle"] = e.PuzzleId.Value,
        }),
        LocationDiscoveredEvent e => new(e.Id, e.OccurredAt, nameof(GameEventKind.LocationDiscovered), new()
        {
            ["location"] = e.LocationId.Value,
        }),
        _ => throw new InvalidOperationException($"Unknown game event type: {gameEvent.GetType().Name}"),
    };

    public static GameEvent FromSnapshot(GameEventSnapshot snapshot)
    {
        var kind = Enum.Parse<GameEventKind>(snapshot.Kind);
        return kind switch
        {
            GameEventKind.PlayerMoved => new PlayerMovedEvent(
                snapshot.Id, snapshot.OccurredAt, new LocationId(snapshot.Payload["from"]), new LocationId(snapshot.Payload["to"])),
            GameEventKind.ItemTaken => new ItemTakenEvent(
                snapshot.Id, snapshot.OccurredAt, new ItemId(snapshot.Payload["item"]), new LocationId(snapshot.Payload["location"])),
            GameEventKind.ItemUsed => new ItemUsedEvent(
                snapshot.Id, snapshot.OccurredAt, new ItemId(snapshot.Payload["item"]),
                snapshot.Payload.TryGetValue("target", out var target) ? new ItemId(target) : null),
            GameEventKind.ObjectStateChanged => new ObjectStateChangedEvent(
                snapshot.Id, snapshot.OccurredAt, new ItemId(snapshot.Payload["item"]), (ItemState)int.Parse(snapshot.Payload["state"])),
            GameEventKind.DoorOpened => new DoorOpenedEvent(
                snapshot.Id, snapshot.OccurredAt, new LocationId(snapshot.Payload["location"]), snapshot.Payload["direction"]),
            GameEventKind.NpcRelationshipChanged => new NpcRelationshipChangedEvent(
                snapshot.Id, snapshot.OccurredAt, new NpcId(snapshot.Payload["npc"]), snapshot.Payload["flag"],
                snapshot.Payload.GetValueOrDefault("utterance"), snapshot.Payload.GetValueOrDefault("reply")),
            GameEventKind.PuzzleSolved => new PuzzleSolvedEvent(snapshot.Id, snapshot.OccurredAt, new PuzzleId(snapshot.Payload["puzzle"])),
            GameEventKind.LocationDiscovered => new LocationDiscoveredEvent(snapshot.Id, snapshot.OccurredAt, new LocationId(snapshot.Payload["location"])),
            _ => throw new InvalidOperationException($"Unknown game event kind: {snapshot.Kind}"),
        };
    }

    private static Dictionary<string, string> BuildItemUsedPayload(ItemUsedEvent e)
    {
        var payload = new Dictionary<string, string> { ["item"] = e.ItemId.Value };
        if (e.TargetId is not null)
        {
            payload["target"] = e.TargetId.Value.Value;
        }

        return payload;
    }

    private static Dictionary<string, string> BuildNpcPayload(NpcRelationshipChangedEvent e)
    {
        var payload = new Dictionary<string, string> { ["npc"] = e.NpcId.Value, ["flag"] = e.FlagKey };
        if (e.PlayerUtterance is not null)
        {
            payload["utterance"] = e.PlayerUtterance;
        }

        if (e.NpcReply is not null)
        {
            payload["reply"] = e.NpcReply;
        }

        return payload;
    }
}
