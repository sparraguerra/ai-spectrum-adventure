namespace AI.SpectrumAdventure.Domain.Players;

using AI.SpectrumAdventure.Domain.Common;

/// <summary>The player (spec Key Entity "Player").</summary>
public sealed class Player
{
    private readonly HashSet<string> _knownClues = [];
    private readonly List<LocationId> _previousLocationIds = [];

    public LocationId CurrentLocationId { get; private set; }
    public Inventory Inventory { get; }
    public IReadOnlySet<string> KnownClues => _knownClues;
    public IReadOnlyList<LocationId> PreviousLocationIds => _previousLocationIds;

    public Player(LocationId startingLocationId)
    {
        CurrentLocationId = startingLocationId;
        Inventory = new Inventory();
    }

    internal void MoveTo(LocationId locationId) => CurrentLocationId = locationId;

    internal void RecordPreviousLocation(LocationId locationId) => _previousLocationIds.Add(locationId);

    internal void BacktrackTo(LocationId fromLocationId, LocationId toLocationId)
    {
        if (CurrentLocationId != fromLocationId)
        {
            throw new InvalidOperationException("Backtracking event origin does not match the player's current location.");
        }

        if (_previousLocationIds.Count == 0 || _previousLocationIds[^1] != toLocationId)
        {
            throw new InvalidOperationException("Backtracking destination does not match the latest previous location.");
        }

        _previousLocationIds.RemoveAt(_previousLocationIds.Count - 1);
        CurrentLocationId = toLocationId;
    }

    internal void RehydratePreviousLocations(IEnumerable<LocationId> previousLocationIds)
    {
        _previousLocationIds.Clear();
        _previousLocationIds.AddRange(previousLocationIds);
    }

    internal void LearnClue(string clueKey) => _knownClues.Add(clueKey);
}
