namespace AI.SpectrumAdventure.Agents.Intent;

/// <summary>Shared vocabulary (directions, item/NPC/puzzle-target synonyms) used by both the deterministic
/// pattern matcher and the keyword fallback classifier, so both resolve free text to the same canonical ids.</summary>
internal static class WorldVocabulary
{
    private static readonly Dictionary<string, string> Directions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["north"] = "North",
        ["n"] = "North",
        ["south"] = "South",
        ["s"] = "South",
        ["east"] = "East",
        ["e"] = "East",
        ["west"] = "West",
        ["w"] = "West",
    };

    private static readonly Dictionary<string, string> Targets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sign"] = "sign",
        ["weathered sign"] = "sign",
        ["key"] = "bridge-key",
        ["bridge key"] = "bridge-key",
        ["rusted key"] = "bridge-key",
        ["rusted bridge key"] = "bridge-key",
        ["iron key"] = "bridge-key",
        ["hermit"] = "hermit",
        ["the hermit"] = "hermit",
        ["old hermit"] = "hermit",
        ["old man"] = "hermit",
        ["tower"] = "forgotten-tower-entrance",
        ["forgotten tower"] = "forgotten-tower-entrance",
        ["door"] = "forgotten-tower-entrance",
        ["tower door"] = "forgotten-tower-entrance",
        ["entrance"] = "forgotten-tower-entrance",
        ["tower entrance"] = "forgotten-tower-entrance",
    };

    public static bool TryResolveDirection(string text, out string direction) =>
        Directions.TryGetValue(text.Trim(), out direction!);

    public static bool TryResolveTarget(string text, out string targetId)
    {
        var cleaned = text.Trim();
        if (cleaned.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[4..].Trim();
        }

        return Targets.TryGetValue(cleaned, out targetId!);
    }

    /// <summary>Best-effort scan for any known target phrase appearing anywhere in free-form text (used by the keyword fallback).</summary>
    public static bool TryFindTargetAnywhere(string lowerText, out string targetId)
    {
        foreach (var (phrase, id) in Targets.OrderByDescending(kv => kv.Key.Length))
        {
            if (lowerText.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                targetId = id;
                return true;
            }
        }

        targetId = string.Empty;
        return false;
    }

    public static bool TryFindDirectionAnywhere(string lowerText, out string direction)
    {
        foreach (var (word, canonical) in Directions.OrderByDescending(kv => kv.Key.Length))
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(lowerText, $@"\b{System.Text.RegularExpressions.Regex.Escape(word)}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                direction = canonical;
                return true;
            }
        }

        direction = string.Empty;
        return false;
    }
}
