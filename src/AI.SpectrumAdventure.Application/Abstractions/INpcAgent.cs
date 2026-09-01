namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Contracts;

/// <summary>Generates in-character NPC dialogue constrained to the NPC's authoritative KnowledgeBoundary (FR-016/FR-017).
/// MUST NOT reveal information outside that boundary or mutate GameState directly.</summary>
public interface INpcAgent
{
    Task<NpcResponse> ReplyAsync(NpcContext context, CancellationToken cancellationToken = default);
}
