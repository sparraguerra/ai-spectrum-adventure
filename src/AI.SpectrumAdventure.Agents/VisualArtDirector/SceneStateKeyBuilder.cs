namespace AI.SpectrumAdventure.Agents.VisualArtDirector;

using AI.SpectrumAdventure.Contracts;

/// <summary>Computes a deterministic cache key (locationId + visible objects + relevant world-flag state) so unchanged scenes are
/// never regenerated (constitution Principle X; research.md Decisions 5/9).</summary>
public static class SceneStateKeyBuilder
{
    public static string Build(Guid worldId, string locationId, int sceneVersion, IEnumerable<string> relevantFlagKeys, IEnumerable<string>? visibleObjectIds = null) =>
        $"world:{worldId:N}|location:{locationId}|scene:{sceneVersion}|" + Build(locationId, relevantFlagKeys, visibleObjectIds);

    public static string Build(string locationId, IEnumerable<string> relevantFlagKeys, IEnumerable<string>? visibleObjectIds = null)
    {
        var sortedFlags = relevantFlagKeys.OrderBy(k => k, StringComparer.Ordinal);
        var sortedVisibleObjectIds = (visibleObjectIds ?? []).OrderBy(id => id, StringComparer.Ordinal);
        return $"{locationId}|objects:{string.Join(',', sortedVisibleObjectIds)}|flags:{string.Join(',', sortedFlags)}";
    }
}
