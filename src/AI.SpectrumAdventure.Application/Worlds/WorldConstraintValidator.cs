namespace AI.SpectrumAdventure.Application.Worlds;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Worlds;

public sealed class WorldConstraintValidator : IWorldConstraintValidator
{
    public WorldConstraintValidationResult Validate(World world, WorldExpansionCandidate candidate)
    {
        using var activity = WorldTelemetry.Start("validation");
        WorldConstraintValidationResult Invalid(string reason)
        {
            activity?.SetTag("world.validation_success", false);
            activity?.SetTag("world.failure_reason", reason);
            return WorldConstraintValidationResult.Invalid(reason);
        }

        if (world.HasGenerationKey(candidate.Metadata.GenerationKey)) return Invalid("That path has already been established.");
        if (world.Locations.Any(location => location.Id == candidate.Location.Id)) return Invalid("The generated location identity already exists.");
        if (world.Connections.Any(connection => connection.Id == candidate.Connection.Id)) return Invalid("The generated connection identity already exists.");
        if (world.FindConnection(candidate.Metadata.SourceLocationId, candidate.Metadata.Direction) is not null) return Invalid("That direction is already connected.");
        if (candidate.Connection.SourceLocationId != candidate.Metadata.SourceLocationId || candidate.Connection.DestinationLocationId != candidate.Location.Id || candidate.Connection.Direction != candidate.Metadata.Direction) return Invalid("The generated connection does not match its boundary.");
        if (candidate.Metadata.WorldSeed != world.Seed || candidate.Metadata.GenerationVersion != world.GenerationVersion) return Invalid("The candidate belongs to a different world.");

        var source = world.GetLocation(candidate.Metadata.SourceLocationId);
        if (!Domain.Worlds.WorldGenerationRules.IsTerrainTransitionPermitted(source.Environment.Terrain, candidate.Location.Environment.Terrain)) return Invalid("The terrain transition is not permitted.");
        if (candidate.Region is not null && candidate.Region.Id != candidate.Location.RegionId) return Invalid("The generated location has an invalid region relationship.");
        activity?.SetTag("world.validation_success", true);
        return WorldConstraintValidationResult.Valid();
    }
}