namespace AI.SpectrumAdventure.Domain.Players;

using AI.SpectrumAdventure.Domain.Common;

/// <summary>The player (spec Key Entity "Player").</summary>
public sealed class Player
{
    private readonly HashSet<string> _knownClues = [];

    public LocationId CurrentLocationId { get; private set; }
    public Inventory Inventory { get; }
    public IReadOnlySet<string> KnownClues => _knownClues;

    public Player(LocationId startingLocationId)
    {
        CurrentLocationId = startingLocationId;
        Inventory = new Inventory();
    }

    internal void MoveTo(LocationId locationId) => CurrentLocationId = locationId;

    internal void LearnClue(string clueKey) => _knownClues.Add(clueKey);
}
