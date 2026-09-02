namespace AI.SpectrumAdventure.Contracts;

/// <summary>NPC Agent input context, restricted to the NPC's authoritative KnowledgeBoundary (FR-017).</summary>
public sealed record NpcContext(
    string NpcName,
    string PersonalityProfile,
    IReadOnlyCollection<string> KnowledgeBoundary,
    IReadOnlyCollection<string> RecentConversation,
    string PlayerUtterance,
    IReadOnlyCollection<string>? PlayerKnownFacts = null,
    string? CurrentLocationId = null);

/// <summary>NPC Agent output (per contracts/npc-response.schema.json).</summary>
public sealed record NpcResponse(
    string NpcId,
    string Reply,
    IReadOnlyCollection<string> RevealedKnowledgeKeys,
    IReadOnlyCollection<string>? SuggestedTopics);
