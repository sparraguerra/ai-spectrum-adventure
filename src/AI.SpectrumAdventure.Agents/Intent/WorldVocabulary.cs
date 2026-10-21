namespace AI.SpectrumAdventure.Agents.Intent;

using System.Text.RegularExpressions;

/// <summary>Shared directions and adventure-provided target synonyms used by both intent parsing paths.</summary>
public sealed class WorldVocabulary
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
        ["up"] = "Up",
        ["down"] = "Down",
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
        if (TryResolveKnownTarget(text, out targetId))
        {
            return true;
        }

        // Adventure-authored items use stable ids, so simple names can resolve without a hard-coded vocabulary entry.
        var cleaned = NormalizeTarget(text);
        targetId = Regex.Replace(cleaned.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return targetId.Length > 0;
    }

    public bool TryResolveKnownTarget(string text, out string targetId)
    {
        var cleaned = NormalizeTarget(text);
        if (targets.TryGetValue(cleaned, out targetId!))
        {
            return true;
        }

        targetId = targets.Values.FirstOrDefault(value => string.Equals(value, cleaned, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        if (targetId.Length > 0)
        {
            return true;
        }

        targetId = string.Empty;
        return false;
    }

    private static string NormalizeTarget(string text)
    {
        var cleaned = text.Trim();
        if (cleaned.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
        {
            return cleaned[4..].Trim();
        }

        return cleaned;
    }

}
