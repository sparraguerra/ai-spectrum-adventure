namespace AI.SpectrumAdventure.Domain.Games;

/// <summary>A persistent fact about the world, resulting from player action (spec Key Entity "World Event/Flag").</summary>
public sealed record WorldFlag(string Key, DateTimeOffset SetAt);

/// <summary>Canonical, well-known world flag keys shared by Domain, Application, and Agents.</summary>
public static class WorldFlags
{
    public const string SignRead = "sign-read";
    public const string TowerUnlocked = "tower-unlocked";
    public const string NpcGaveClue = "npc-gave-clue";
}
