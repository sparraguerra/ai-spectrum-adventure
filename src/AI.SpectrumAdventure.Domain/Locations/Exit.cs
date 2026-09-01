namespace AI.SpectrumAdventure.Domain.Locations;

using AI.SpectrumAdventure.Domain.Common;

/// <summary>A condition gating movement or item visibility, evaluated against the current set of active world flag keys.</summary>
public sealed record WorldFlagCondition(string RequiredFlagKey, bool MustBeSet = true)
{
    public bool IsSatisfiedBy(IReadOnlySet<string> activeFlagKeys) =>
        activeFlagKeys.Contains(RequiredFlagKey) == MustBeSet;
}

/// <summary>A connection from one Location to another, optionally gated by a WorldFlagCondition (FR-007/FR-008).</summary>
public sealed record Exit(string Direction, LocationId DestinationId, WorldFlagCondition? RequiredCondition = null);
