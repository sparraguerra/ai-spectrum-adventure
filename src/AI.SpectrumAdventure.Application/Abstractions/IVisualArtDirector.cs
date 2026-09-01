namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Contracts;

/// <summary>Transforms validated scene state into a retro 8-bit visual specification. MUST NOT mutate GameState
/// or become the authoritative scene representation (constitution Principle XXI).</summary>
public interface IVisualArtDirector
{
    Task<VisualSceneSpec> DescribeSceneAsync(VisualContext context, CancellationToken cancellationToken = default);
}
