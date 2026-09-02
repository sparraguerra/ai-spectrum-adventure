namespace AI.SpectrumAdventure.Domain.Worlds;

using AI.SpectrumAdventure.Domain.Common;

public sealed class WorldConnection
{
    public ConnectionId Id { get; }
    public LocationId SourceLocationId { get; }
    public LocationId DestinationLocationId { get; }
    public ConnectionDirection Direction { get; }
    public ConnectionVisibility Visibility { get; private set; }
    public ConnectionAvailability Availability { get; private set; }

    public WorldConnection(ConnectionId id, LocationId sourceLocationId, LocationId destinationLocationId, ConnectionDirection direction, ConnectionVisibility visibility = ConnectionVisibility.Hidden, ConnectionAvailability availability = ConnectionAvailability.Available)
    {
        if (sourceLocationId == destinationLocationId) throw new ArgumentException("A connection must link two distinct locations.");

        Id = id;
        SourceLocationId = sourceLocationId;
        DestinationLocationId = destinationLocationId;
        Direction = direction;
        Visibility = visibility;
        Availability = availability;
    }

    internal void SetState(ConnectionVisibility visibility, ConnectionAvailability availability)
    {
        Visibility = visibility;
        Availability = availability;
    }
}