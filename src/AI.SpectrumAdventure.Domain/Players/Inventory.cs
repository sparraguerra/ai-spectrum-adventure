namespace AI.SpectrumAdventure.Domain.Players;

using AI.SpectrumAdventure.Domain.Common;

/// <summary>The set of items currently possessed by the player (spec Key Entity "Inventory").</summary>
public sealed class Inventory
{
    private readonly HashSet<ItemId> _items = [];

    public IReadOnlyCollection<ItemId> Items => _items;

    public bool Contains(ItemId itemId) => _items.Contains(itemId);

    public void Add(ItemId itemId) => _items.Add(itemId);

    public bool Remove(ItemId itemId) => _items.Remove(itemId);
}
