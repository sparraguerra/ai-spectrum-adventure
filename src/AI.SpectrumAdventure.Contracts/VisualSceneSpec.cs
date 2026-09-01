namespace AI.SpectrumAdventure.Contracts;

/// <summary>Visual Art Director input context (never raw GameState — only what's visually relevant).</summary>
public sealed record VisualContext(
    string LocationId,
    string LocationName,
    IReadOnlyCollection<string> VisibleObjectNames,
    string Mood,
    string SceneStateKey);

/// <summary>Visual Art Director output (per contracts/visual-scene-spec.schema.json).</summary>
public sealed record VisualSceneSpec(
    string LocationId,
    string SceneStateKey,
    string? TimeOfDay,
    IReadOnlyCollection<string> Objects,
    string Mood,
    string Style);
