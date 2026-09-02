namespace AI.SpectrumAdventure.Application.Worlds;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Worlds;

public sealed record WorldEventScheduleRequest(WorldEventCause Cause, string Detail, long Turn, DateTimeOffset OccurredAt);

/// <summary>Selects only predeclared state changes from persisted world state; it never delegates authority to an agent.</summary>
public sealed class WorldEventScheduler
{
    public WorldEvent? Select(World world, WorldEventScheduleRequest request)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(request);
        if (request.Turn < 0 || string.IsNullOrWhiteSpace(request.Detail)) throw new ArgumentException("A non-negative turn and event detail are required.", nameof(request));

        if (request.Cause == WorldEventCause.PlayerAction && request.Detail.StartsWith("block:", StringComparison.OrdinalIgnoreCase))
        {
            var connectionId = new ConnectionId(request.Detail["block:".Length..]);
            var connection = world.Connections.SingleOrDefault(candidate => candidate.Id == connectionId);
            return connection is null || connection.Availability == ConnectionAvailability.Blocked
                ? null
                : new ConnectionStateChangedWorldEvent(WorldEventId.New(), world.Events.Count + 1, request.OccurredAt, WorldEvent.FormatCause(request.Cause, request.Detail), connection.Id, connection.Visibility, ConnectionAvailability.Blocked);
        }

        if (request.Cause != WorldEventCause.TurnElapsed) return null;

        var available = world.Connections.Where(connection => connection.Availability == ConnectionAvailability.Available)
            .OrderBy(connection => connection.Id.Value, StringComparer.Ordinal).ToArray();
        if (available.Length == 0) return null;

        var selected = available[(int)(request.Turn % available.Length)];
        return new ConnectionStateChangedWorldEvent(
            WorldEventId.New(),
            world.Events.Count + 1,
            request.OccurredAt,
            WorldEvent.FormatCause(request.Cause, request.Detail),
            selected.Id,
            selected.Visibility,
            ConnectionAvailability.Blocked);
    }
}