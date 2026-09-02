namespace AI.SpectrumAdventure.Application.Worlds;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Worlds;

public sealed record UnknownDirectionExplorationResult(bool Success, string Narrative);

public sealed class ExploreUnknownDirectionUseCase(
    IWorldRepository worldRepository,
    IRegionGenerator regionGenerator,
    ILocationGenerator locationGenerator,
    IConnectionGenerator connectionGenerator,
    IWorldGenerationRules rules,
    IWorldConstraintValidator constraintValidator,
    WorldEnrichmentService? enrichmentService = null)
{
    public async Task<UnknownDirectionExplorationResult> ExecuteAsync(Game game, string direction, CancellationToken cancellationToken = default)
    {
        using var activity = WorldTelemetry.Start("generation");
        activity?.SetTag("world.direction", direction);
        if (game.WorldId is not { } worldId || !Enum.TryParse<ConnectionDirection>(direction, true, out var parsedDirection))
        {
            activity?.SetTag("world.generation_success", false);
            activity?.SetTag("world.failure_reason", "invalid_direction");
            return new(false, "There is no path in that direction.");
        }

        var world = await worldRepository.FindAsync(worldId, cancellationToken);
        if (world is null)
        {
            activity?.SetTag("world.generation_success", false);
            activity?.SetTag("world.failure_reason", "world_not_found");
            return new(false, "The way ahead cannot be found right now.");
        }
        if (!rules.CanExpand(world, game.Player.CurrentLocationId, parsedDirection))
        {
            activity?.SetTag("world.generation_success", false);
            activity?.SetTag("world.failure_reason", "cannot_expand");
            return new(false, "There is no unexplored path in that direction.");
        }

        var ordinal = world.GenerationMetadata.Count(metadata => metadata.SourceLocationId == game.Player.CurrentLocationId && metadata.Direction == parsedDirection);
        var generationKey = DeterministicGenerationKeyFactory.Create(world.Seed, world.GenerationVersion, game.Player.CurrentLocationId, parsedDirection, ordinal);
        var region = regionGenerator.Generate(new WorldGenerationRequest(world, game.Player.CurrentLocationId, parsedDirection, ordinal), generationKey);
        var request = new WorldGenerationRequest(world, game.Player.CurrentLocationId, parsedDirection, ordinal);
        var location = locationGenerator.Generate(request, generationKey, region);
        var connection = connectionGenerator.Generate(request, generationKey, location);
        var metadata = new GeneratedContentMetadata(generationKey, world.Seed, world.GenerationVersion, game.Player.CurrentLocationId, parsedDirection, ordinal, "exploration", DateTimeOffset.UnixEpoch);
        var candidate = new WorldExpansionCandidate(region, location, connection, metadata);
        var validation = constraintValidator.Validate(world, candidate);
        if (!validation.IsValid)
        {
            activity?.SetTag("world.generation_success", false);
            activity?.SetTag("world.failure_reason", validation.FailureReason);
            return new(false, validation.FailureReason!);
        }

        var expansion = new WorldExpandedEvent(WorldEventId.New(), world.Events.Count + 1, DateTimeOffset.UtcNow, WorldEvent.FormatCause(WorldEventCause.PlayerAction, "exploration"), candidate.Region, candidate.Location, candidate.Connection, candidate.Metadata);
        try
        {
            var expandedWorld = CloneForExpansion(world);
            expandedWorld.Apply(expansion);
            var persistedWorld = await worldRepository.PersistExpansionAsync(expandedWorld, expansion, cancellationToken);
            var persistedConnection = persistedWorld.FindConnection(game.Player.CurrentLocationId, parsedDirection)!;
            var destination = persistedWorld.GetLocation(persistedConnection.DestinationLocationId);
            game.AddGeneratedLocation(destination, persistedConnection);
            var now = DateTimeOffset.UtcNow;
            game.Apply(new PlayerMovedEvent(Guid.NewGuid(), now, persistedConnection.SourceLocationId, persistedConnection.DestinationLocationId));
            game.Apply(new LocationDiscoveredEvent(Guid.NewGuid(), now, persistedConnection.DestinationLocationId));
            game.PlayerKnowledge.DiscoverRegion(destination.RegionId, now);
            game.PlayerKnowledge.DiscoverLocation(destination.Id, now);
            game.PlayerKnowledge.DiscoverConnection(persistedConnection.Id, now);
            await worldRepository.SaveWorldAndGameAsync(persistedWorld, game, cancellationToken);
            if (enrichmentService is not null)
            {
                try
                {
                    await enrichmentService.EnrichAsync(persistedWorld, destination, cancellationToken);
                }
                catch (Exception)
                {
                    // Presentation enrichment is intentionally non-blocking after authoritative persistence.
                    activity?.SetTag("world.enrichment_success", false);
                }
            }
            activity?.SetTag("world.generation_success", true);
            activity?.SetTag("world.location_id", destination.Id.Value);
            return new(true, destination.BaseDescription);
        }
        catch (Exception)
        {
            activity?.SetTag("world.generation_success", false);
            activity?.SetTag("world.failure_reason", "persistence_failure");
            return new(false, "The way ahead remains uncertain. You can still travel the known paths.");
        }
    }

    private static World CloneForExpansion(World world)
    {
        var regions = world.Regions.Select(region => new Region(region.Id, region.Type, region.TerrainProfile, region.ExpansionPolicy, region.LocationIds));
        return new World(world.Id, world.Seed, world.GenerationVersion, regions, world.Locations, world.Connections, world.GenerationMetadata, world.Events);
    }
}