namespace AI.SpectrumAdventure.Agents.Intent;

using AI.SpectrumAdventure.Contracts;

/// <summary>
/// A lightweight, no-LLM-call keyword scan for free natural language that doesn't match a classic command shape
/// (spec FR-005). This is an interim, deterministic stand-in for the LLM-backed classifier described in
/// research.md Decision 3; it is replaced by the real Microsoft Agent Framework agent once Phase 9/10 wires
/// the model provider in, without requiring any change to TwoStageIntentInterpreter's public contract.
/// </summary>
internal static class KeywordFallbackClassifier
{
    private static readonly (string[] Keywords, IntentAction Action)[] ActionKeywords =
    [
        (["examine", "inspect", "look at", "check out", "study"], IntentAction.Examine),
        (["take", "grab", "pick up", "collect", "get the"], IntentAction.Take),
        (["open", "pry open", "force open"], IntentAction.Open),
        (["talk", "speak", "ask", "converse", "chat"], IntentAction.TalkTo),
        (["use", "unlock", "insert", "apply"], IntentAction.Use),
        (["go", "walk", "head", "travel", "move", "run"], IntentAction.Go),
    ];

    public static ParsedIntent Classify(string rawInput)
    {
        var lower = rawInput.ToLowerInvariant();

        foreach (var (keywords, action) in ActionKeywords)
        {
            if (!keywords.Any(k => lower.Contains(k, StringComparison.Ordinal)))
            {
                continue;
            }

            if (action == IntentAction.Go)
            {
                return WorldVocabulary.TryFindDirectionAnywhere(lower, out var direction)
                    ? new ParsedIntent(action, direction, new Dictionary<string, string>(), 0.6, rawInput)
                    : new ParsedIntent(action, null, new Dictionary<string, string>(), 0.3, rawInput);
            }

            if (!WorldVocabulary.TryFindTargetAnywhere(lower, out var targetId))
            {
                // Action recognized, but no known target/object phrase found — genuinely ambiguous (Edge Case).
                return new ParsedIntent(action, null, new Dictionary<string, string>(), 0.3, rawInput);
            }

            var parameters = new Dictionary<string, string>();
            if (action == IntentAction.Use && lower.Contains(" on ", StringComparison.Ordinal))
            {
                var afterOn = lower[(lower.IndexOf(" on ", StringComparison.Ordinal) + 4)..];
                if (WorldVocabulary.TryFindTargetAnywhere(afterOn, out var onTarget) && onTarget != targetId)
                {
                    parameters["on"] = onTarget;
                }
            }

            return new ParsedIntent(action, targetId, parameters, 0.6, rawInput);
        }

        return ParsedIntent.Unknown(rawInput);
    }
}
