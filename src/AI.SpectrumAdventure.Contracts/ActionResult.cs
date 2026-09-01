namespace AI.SpectrumAdventure.Contracts;

/// <summary>The player-facing outcome of one processed action (per contracts/action-result.schema.json).</summary>
public sealed record ActionResult(
    Guid GameId,
    bool Success,
    string? Reason,
    string Narrative,
    bool SceneChanged,
    string CurrentLocationId,
    IReadOnlyCollection<string> InventoryItemIds,
    string? VisualAssetUrl);
