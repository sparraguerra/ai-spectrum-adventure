namespace AI.SpectrumAdventure.Application.Games;

using AI.SpectrumAdventure.Domain.Games;

/// <summary>FR-012: lists exactly the items currently possessed by the player.</summary>
public static class GetInventoryQuery
{
    public static IReadOnlyCollection<string> Execute(Game game) =>
        [.. game.Player.Inventory.Items.Select(id => id.Value)];
}
