namespace AI.SpectrumAdventure.Contracts;

/// <summary>Narrator input context: validated action outcome + minimal relevant world facts (never a full GameState dump).</summary>
public sealed record NarratorContext(
    string LocationName,
    string LocationDescription,
    IReadOnlyCollection<string> VisibleObjectNames,
    IReadOnlyCollection<string> PresentCharacterNames,
    IReadOnlyCollection<string> AvailableExits,
    bool ActionSucceeded,
    string? FailureReason,
    string ActionSummary);

/// <summary>Narrator output (per contracts/narration-result.schema.json).</summary>
public sealed record NarrationResult(string Narration, string? Mood, bool SceneChanged, IReadOnlyCollection<string> ImportantEvents);
