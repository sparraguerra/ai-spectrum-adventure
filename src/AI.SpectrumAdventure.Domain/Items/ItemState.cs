namespace AI.SpectrumAdventure.Domain.Items;

/// <summary>Supports FR-009: an item may have multiple states simultaneously (e.g., Visible | Collectible).</summary>
[Flags]
public enum ItemState
{
    None = 0,
    Visible = 1 << 0,
    Hidden = 1 << 1,
    Collectible = 1 << 2,
    Locked = 1 << 3,
    Movable = 1 << 4,
    Interactive = 1 << 5,
}
