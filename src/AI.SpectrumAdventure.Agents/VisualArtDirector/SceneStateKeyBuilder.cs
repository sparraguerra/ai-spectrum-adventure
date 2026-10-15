namespace AI.SpectrumAdventure.Agents.VisualArtDirector;

using AI.SpectrumAdventure.Contracts;

/// <summary>Computes a deterministic cache key scoped to an adventure version and material scene state, not a playthrough or world instance.</summary>
public static class SceneStateKeyBuilder
{
    public static string Build(
        string adventureId,
        Guid? adventureVersionId,
        string locationId,
        int sceneVersion,
        IEnumerable<string> relevantFlagKeys,
        IEnumerable<string>? visibleObjectIds = null)
    {
        var sortedFlags = relevantFlagKeys.OrderBy(k => k, StringComparer.Ordinal);
        var sortedVisibleObjectIds = (visibleObjectIds ?? []).OrderBy(id => id, StringComparer.Ordinal);
        var version = adventureVersionId?.ToString("N") ?? "unversioned";
        return $"adventure:{adventureId}|version:{version}|location:{locationId}|scene:{sceneVersion}|objects:{string.Join(',', sortedVisibleObjectIds)}|flags:{string.Join(',', sortedFlags)}";
    }
}
