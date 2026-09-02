namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Worlds;

public sealed record WorldGenerationRequest(World World, LocationId SourceLocationId, ConnectionDirection Direction, int ExpansionOrdinal);
public sealed record WorldExpansionCandidate(Region? Region, WorldLocation Location, WorldConnection Connection, GeneratedContentMetadata Metadata);
public sealed record WorldConstraintValidationResult(bool IsValid, string? FailureReason)
{
    public static WorldConstraintValidationResult Valid() => new(true, null);
    public static WorldConstraintValidationResult Invalid(string reason) => new(false, reason);
}

public interface IRegionGenerator { Region? Generate(WorldGenerationRequest request, GenerationKey generationKey); }
public interface ILocationGenerator { WorldLocation Generate(WorldGenerationRequest request, GenerationKey generationKey, Region? region); }
public interface IConnectionGenerator { WorldConnection Generate(WorldGenerationRequest request, GenerationKey generationKey, WorldLocation location); }
public interface IWorldGenerationRules { bool CanExpand(World world, LocationId sourceLocationId, ConnectionDirection direction); }
public interface IWorldConstraintValidator { WorldConstraintValidationResult Validate(World world, WorldExpansionCandidate candidate); }