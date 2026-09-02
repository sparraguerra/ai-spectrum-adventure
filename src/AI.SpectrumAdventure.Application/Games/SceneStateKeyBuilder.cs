namespace AI.SpectrumAdventure.Application.Games;

/// <summary>Deterministic cache key (locationId + visible objects + relevant world-flag state) — see Agents' SceneStateKeyBuilder
/// for the identical logic used by the Visual Art Director; duplicated here since Application must not depend on Agents.</summary>
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
