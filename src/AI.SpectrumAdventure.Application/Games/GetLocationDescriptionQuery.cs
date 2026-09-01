namespace AI.SpectrumAdventure.Application.Games;

using System.Text;
using AI.SpectrumAdventure.Domain.Games;

/// <summary>FR-002: a deterministic, templated location description (atmosphere, objects, characters, exits).
/// Replaced by Narrator-generated prose in Phase 10; kept as the safe fallback thereafter.</summary>
public static class GetLocationDescriptionQuery
{
    public static string Execute(Game game)
    {
        var location = game.CurrentLocation;
        var sb = new StringBuilder(location.BaseDescription);

        var visibleObjectNames = location.ObjectIds
            .Select(game.GetItem)
            .Where(item => item.LocationId == location.Id && item.IsRevealed(game.WorldFlagKeys))
            .Select(item => item.Name)
            .ToList();
        if (visibleObjectNames.Count > 0)
        {
            sb.Append(" You notice: ").Append(string.Join(", ", visibleObjectNames)).Append('.');
        }

        var npcNames = location.NpcIds.Select(game.GetNpc).Select(npc => npc.Name).ToList();
        if (npcNames.Count > 0)
        {
            sb.Append(' ').Append(string.Join(", ", npcNames)).Append(" is here.");
        }

        var availableExits = location.Exits
            .Where(exit => exit.RequiredCondition is null || exit.RequiredCondition.IsSatisfiedBy(game.WorldFlagKeys))
            .Select(exit => exit.Direction)
            .ToList();
        if (availableExits.Count > 0)
        {
            sb.Append(" Exits: ").Append(string.Join(", ", availableExits)).Append('.');
        }

        return sb.ToString();
    }
}
