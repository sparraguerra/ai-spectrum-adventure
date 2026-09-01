namespace AI.SpectrumAdventure.Domain.Npcs;

/// <summary>One turn of dialogue between the player and an NPC, already validated against the NPC's KnowledgeBoundary.</summary>
public sealed record ConversationTurn(string PlayerUtterance, string NpcReply, DateTimeOffset Timestamp);
