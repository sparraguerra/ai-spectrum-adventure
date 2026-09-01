namespace AI.SpectrumAdventure.Contracts;

/// <summary>The recognized action shapes the Rules Engine can validate. "Unknown" means no reasonable
/// interpretation was found (FR-006: respond with a meaningful in-world message, not a technical error).</summary>
public enum IntentAction
{
    Look,
    Examine,
    Go,
    Take,
    Open,
    Use,
    TalkTo,
    Unknown,
}

/// <summary>
/// The structured result of interpreting player input (classic command or natural language) — spec FR-004/FR-005.
/// Producing a ParsedIntent never mutates GameState; only the RulesEngine decides feasibility (constitution Principle IV).
/// </summary>
public sealed record ParsedIntent(
    IntentAction Action,
    string? Target,
    IReadOnlyDictionary<string, string> Parameters,
    double Confidence,
    string RawInput)
{
    public static ParsedIntent Unknown(string rawInput) =>
        new(IntentAction.Unknown, Target: null, Parameters: new Dictionary<string, string>(), Confidence: 0, rawInput);
}
