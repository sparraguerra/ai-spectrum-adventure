namespace AI.SpectrumAdventure.Agents.Intent;

using System.Text.RegularExpressions;

/// <summary>Shared directions and adventure-provided target synonyms used by both intent parsing paths.</summary>
internal sealed class WorldVocabulary
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

    private readonly IReadOnlyDictionary<string, string> targets;

    public static WorldVocabulary Empty { get; } = new(new Dictionary<string, string>());

    public WorldVocabulary(IReadOnlyDictionary<string, string> targets)
    {
        this.targets = new Dictionary<string, string>(targets, StringComparer.OrdinalIgnoreCase);
    }

    public static bool TryResolveDirection(string text, out string direction) =>
        Directions.TryGetValue(text.Trim(), out direction!);

    public bool TryResolveTarget(string text, out string targetId)
    {
        var cleaned = text.Trim();
        if (cleaned.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[4..].Trim();
        }

        if (targets.TryGetValue(cleaned, out targetId!))
        {
            return true;
        }

        // Adventure-authored items use stable ids, so simple names can resolve without a hard-coded vocabulary entry.
        targetId = Regex.Replace(cleaned.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return targetId.Length > 0;
    }

    /// <summary>Best-effort scan for any known target phrase appearing anywhere in free-form text (used by the keyword fallback).</summary>
    public bool TryFindTargetAnywhere(string lowerText, out string targetId)
    {
        foreach (var (phrase, id) in targets.OrderByDescending(kv => kv.Key.Length))
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
