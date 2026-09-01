namespace AI.SpectrumAdventure.Domain.Items;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Locations;

/// <summary>An interactive or collectible object in the world (spec Key Entity "Object/Item").</summary>
public sealed class GameItem
{
    public ItemId Id { get; }
    public string Name { get; }
    public string Description { get; }
    public ItemState State { get; private set; }
    public LocationId? LocationId { get; private set; }
    public WorldFlagCondition? RevealCondition { get; }

    public GameItem(
        ItemId id,
        string name,
        string description,
        ItemState state,
        LocationId? locationId,
        WorldFlagCondition? revealCondition = null)
    {
        Id = id;
        Name = name;
        Description = description;
        State = state;
        LocationId = locationId;
        RevealCondition = revealCondition;
    }

    public bool HasState(ItemState flag) => (State & flag) == flag;

    /// <summary>Whether this item should currently appear in descriptions/examine results, given active world flags.</summary>
    public bool IsRevealed(IReadOnlySet<string> activeFlagKeys)
    {
        if (!HasState(ItemState.Hidden))
        {
            return true;
        }

        return RevealCondition is not null && RevealCondition.IsSatisfiedBy(activeFlagKeys);
    }

    internal void SetState(ItemState newState) => State = newState;

    internal void MoveTo(LocationId? locationId) => LocationId = locationId;
}
