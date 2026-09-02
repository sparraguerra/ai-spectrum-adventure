namespace AI.SpectrumAdventure.Application.Worlds;

using AI.SpectrumAdventure.Contracts;

public sealed class WorldEnrichmentValidator
{
    public WorldEnrichmentValidationResult Validate(WorldEnrichmentContext context, WorldEnrichmentProposal proposal)
    {
        if (!string.Equals(context.LocationId, proposal.LocationId, StringComparison.Ordinal))
            return WorldEnrichmentValidationResult.Invalid("The proposal belongs to a different location.");
        if (context.SceneVersion != proposal.SceneVersion)
            return WorldEnrichmentValidationResult.Invalid("The proposal belongs to a different scene version.");
        if (proposal.VisualCharacteristics.Any(characteristic => !context.AllowedVisualCharacteristics.Contains(characteristic, StringComparer.Ordinal)))
            return WorldEnrichmentValidationResult.Invalid("The proposal includes an unapproved visual characteristic.");
        if (proposal.LoreClaimKeys.Any(loreKey => !context.AllowedLoreKeys.Contains(loreKey, StringComparer.Ordinal)))
            return WorldEnrichmentValidationResult.Invalid("The proposal includes an unapproved lore claim.");

        return WorldEnrichmentValidationResult.Valid();
    }
}