namespace AI.SpectrumAdventure.Application.Worlds;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Lore;
using AI.SpectrumAdventure.Domain.Worlds;

public sealed class WorldEnrichmentService(
    IWorldEnrichmentAgent enrichmentAgent,
    IWorldEnrichmentRepository enrichmentRepository,
    WorldEnrichmentValidator validator)
{
    public async Task<WorldPresentationMetadata> EnrichAsync(World world, WorldLocation location, CancellationToken cancellationToken = default)
    {
        using var activity = WorldTelemetry.Start("enrichment");
        var existing = await enrichmentRepository.FindAsync(world.Id.Value, location.Id.Value, location.SceneVersion, cancellationToken);
        if (existing is not null)
        {
            activity?.SetTag("world.enrichment_cache_hit", true);
            return existing;
        }

        var context = BuildContext(world, location);
        try
        {
            var proposal = await enrichmentAgent.EnrichAsync(context, cancellationToken);
            if (!validator.Validate(context, proposal).IsValid)
            {
                activity?.SetTag("world.enrichment_success", false);
                activity?.SetTag("world.failure_reason", "validation_failed");
                return CreateFallback(context);
            }

            var metadata = new WorldPresentationMetadata(
                context.WorldId, context.LocationId, context.SceneVersion, context.FactualName, context.FactualDescription,
                proposal.DisplayName, proposal.Description, proposal.Atmosphere, proposal.VisualCharacteristics);
            await enrichmentRepository.SaveAsync(metadata, cancellationToken);
            activity?.SetTag("world.enrichment_success", true);
            return metadata;
        }
        catch (Exception)
        {
            activity?.SetTag("world.enrichment_success", false);
            activity?.SetTag("world.failure_reason", "agent_failure");
            return CreateFallback(context);
        }
    }

    public static WorldEnrichmentContext BuildContext(World world, WorldLocation location)
    {
        var allowedLoreKeys = world.LoreEntries
            .Where(entry => entry.Scope == LoreScope.World || entry.SubjectReferences.Contains(location.Id.Value) || entry.SubjectReferences.Contains(location.RegionId.Value))
            .Select(entry => entry.ContentKey)
            .ToArray();
        var visualCharacteristics = location.Environment.Features
            .Append(location.Environment.Terrain.ToString())
            .Append(location.Environment.Climate)
            .Append(location.Environment.Lighting)
            .Append(location.Type.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new WorldEnrichmentContext(world.Id.Value, location.Id.Value, location.RegionId.Value, location.SceneVersion,
            location.StructuralNameKey, location.BaseDescription, allowedLoreKeys, visualCharacteristics);
    }

    public static WorldPresentationMetadata CreateFallback(WorldEnrichmentContext context) =>
        new(context.WorldId, context.LocationId, context.SceneVersion, context.FactualName, context.FactualDescription,
            null, null, null, context.AllowedVisualCharacteristics);
}