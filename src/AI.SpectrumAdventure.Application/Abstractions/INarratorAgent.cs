namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Contracts;

/// <summary>Transforms a validated action outcome into engaging narrative text (constitution's Game Director / Narrator).
/// MUST NOT invent facts, override rules, or mutate GameState.</summary>
public interface INarratorAgent
{
    Task<NarrationResult> NarrateAsync(NarratorContext context, CancellationToken cancellationToken = default);
}
