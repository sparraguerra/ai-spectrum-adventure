namespace AI.SpectrumAdventure.Domain.Locations;

using AI.SpectrumAdventure.Domain.Common;

/// <summary>A place in the world (spec Key Entity "Location"); mutation is restricted to internal domain methods.</summary>
public sealed class Location
{
    private readonly List<Exit> _exits;
    private readonly List<ItemId> _objectIds;
    private readonly List<NpcId> _npcIds;

    public LocationId Id { get; }
    public string Name { get; }
    public string BaseDescription { get; }
    public IReadOnlyCollection<Exit> Exits => _exits;
    public IReadOnlyCollection<ItemId> ObjectIds => _objectIds;
    public IReadOnlyCollection<NpcId> NpcIds => _npcIds;
    public bool Discovered { get; private set; }

    public Location(
        LocationId id,
        string name,
        string baseDescription,
        IEnumerable<Exit>? exits = null,
        IEnumerable<ItemId>? objectIds = null,
        IEnumerable<NpcId>? npcIds = null)
    {
        Id = id;
        Name = name;
        BaseDescription = baseDescription;
        _exits = exits?.ToList() ?? [];
        _objectIds = objectIds?.ToList() ?? [];
        _npcIds = npcIds?.ToList() ?? [];
    }

    public Exit? FindExit(string direction) =>
        _exits.FirstOrDefault(e => string.Equals(e.Direction, direction, StringComparison.OrdinalIgnoreCase));

    internal void MarkDiscovered() => Discovered = true;

    internal void AddExit(Exit exit)
    {
        if (FindExit(exit.Direction) is null)
        {
            _exits.Add(exit);
        }
    }

    internal void AddItem(ItemId itemId)
    {
        if (!_objectIds.Contains(itemId))
        {
            _objectIds.Add(itemId);
        }
    }

    internal void RemoveItem(ItemId itemId) => _objectIds.Remove(itemId);
}
