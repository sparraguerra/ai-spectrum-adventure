namespace AI.SpectrumAdventure.Domain.Worlds;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Items;

public enum WorldEventKind
{
    Expansion,
    ConnectionChanged,
    NpcMoved,
    PuzzleConsequence,
    EnvironmentChanged,
    ObjectChanged
}

public enum WorldEventCause
{
    PlayerAction,
    TurnElapsed,
    PuzzleOutcome,
    WorldRule
}

public abstract record WorldEvent(WorldEventId Id, long Sequence, DateTimeOffset OccurredAt, WorldEventKind Kind, string Cause)
{
    internal static void Validate(long sequence, string cause)
    {
        if (sequence < 1) throw new ArgumentOutOfRangeException(nameof(sequence));
        if (string.IsNullOrWhiteSpace(cause)) throw new ArgumentException("An event cause is required.", nameof(cause));
    }

    public static string FormatCause(WorldEventCause cause, string detail)
    {
        if (string.IsNullOrWhiteSpace(detail)) throw new ArgumentException("An event cause detail is required.", nameof(detail));
        return $"{cause}:{detail}";
    }

    public static bool IsDeterministicCause(string cause) =>
        Enum.TryParse<WorldEventCause>(cause.Split(':', 2)[0], true, out _);
}

public sealed record WorldExpandedEvent(
    WorldEventId Id,
    long Sequence,
    DateTimeOffset OccurredAt,
    string Cause,
    Region? Region,
    WorldLocation Location,
    WorldConnection Connection,
    GeneratedContentMetadata Metadata)
    : WorldEvent(Id, Sequence, OccurredAt, WorldEventKind.Expansion, Cause);

public sealed record ConnectionStateChangedWorldEvent(
    WorldEventId Id,
    long Sequence,
    DateTimeOffset OccurredAt,
    string Cause,
    ConnectionId ConnectionId,
    ConnectionVisibility Visibility,
    ConnectionAvailability Availability)
    : WorldEvent(Id, Sequence, OccurredAt, WorldEventKind.ConnectionChanged, Cause);

public sealed record LocationStateChangedWorldEvent(
    WorldEventId Id,
    long Sequence,
    DateTimeOffset OccurredAt,
    string Cause,
    LocationId LocationId,
    LocationState State)
    : WorldEvent(Id, Sequence, OccurredAt, WorldEventKind.EnvironmentChanged, Cause);

public sealed record ObjectStateChangedWorldEvent(
    WorldEventId Id,
    long Sequence,
    DateTimeOffset OccurredAt,
    string Cause,
    ItemId ItemId,
    ItemState State)
    : WorldEvent(Id, Sequence, OccurredAt, WorldEventKind.ObjectChanged, Cause);

public sealed record NpcMovedWorldEvent(
    WorldEventId Id,
    long Sequence,
    DateTimeOffset OccurredAt,
    string Cause,
    NpcId NpcId,
    LocationId LocationId)
    : WorldEvent(Id, Sequence, OccurredAt, WorldEventKind.NpcMoved, Cause);

public sealed record PuzzleStateChangedWorldEvent(
    WorldEventId Id,
    long Sequence,
    DateTimeOffset OccurredAt,
    string Cause,
    PuzzleId PuzzleId,
    bool IsSolved)
    : WorldEvent(Id, Sequence, OccurredAt, WorldEventKind.PuzzleConsequence, Cause);