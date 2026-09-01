namespace AI.SpectrumAdventure.Domain.Events;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Items;

public enum GameEventKind
{
    PlayerMoved,
    ItemTaken,
    ItemUsed,
    ObjectStateChanged,
    DoorOpened,
    NpcRelationshipChanged,
    PuzzleSolved,
    LocationDiscovered,
}

/// <summary>Base type for all controlled, validated world-state changes (spec Key Entity "World Event/Flag" history half).</summary>
public abstract record GameEvent(Guid Id, DateTimeOffset OccurredAt, GameEventKind Kind);

public sealed record PlayerMovedEvent(Guid Id, DateTimeOffset OccurredAt, LocationId FromLocationId, LocationId ToLocationId)
    : GameEvent(Id, OccurredAt, GameEventKind.PlayerMoved);

public sealed record ItemTakenEvent(Guid Id, DateTimeOffset OccurredAt, ItemId ItemId, LocationId LocationId)
    : GameEvent(Id, OccurredAt, GameEventKind.ItemTaken);

public sealed record ItemUsedEvent(Guid Id, DateTimeOffset OccurredAt, ItemId ItemId, ItemId? TargetId)
    : GameEvent(Id, OccurredAt, GameEventKind.ItemUsed);

public sealed record ObjectStateChangedEvent(Guid Id, DateTimeOffset OccurredAt, ItemId ItemId, ItemState NewState)
    : GameEvent(Id, OccurredAt, GameEventKind.ObjectStateChanged);

public sealed record DoorOpenedEvent(Guid Id, DateTimeOffset OccurredAt, LocationId LocationId, string ExitDirection)
    : GameEvent(Id, OccurredAt, GameEventKind.DoorOpened);

public sealed record NpcRelationshipChangedEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    NpcId NpcId,
    string FlagKey,
    string? PlayerUtterance = null,
    string? NpcReply = null)
    : GameEvent(Id, OccurredAt, GameEventKind.NpcRelationshipChanged);

public sealed record PuzzleSolvedEvent(Guid Id, DateTimeOffset OccurredAt, PuzzleId PuzzleId)
    : GameEvent(Id, OccurredAt, GameEventKind.PuzzleSolved);

public sealed record LocationDiscoveredEvent(Guid Id, DateTimeOffset OccurredAt, LocationId LocationId)
    : GameEvent(Id, OccurredAt, GameEventKind.LocationDiscovered);
