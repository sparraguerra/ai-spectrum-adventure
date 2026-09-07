namespace AI.SpectrumAdventure.Domain.Games;

using AI.SpectrumAdventure.Domain.Authoring;

/// <summary>
/// A fully serializable, flat mirror of a Game aggregate's state, used only at the persistence boundary
/// (Infrastructure serializes/deserializes this; the Domain remains unaware of EF Core or JSON).
/// Uses concrete List/Dictionary types (not IReadOnlyCollection) so System.Text.Json can round-trip them.
/// </summary>
public sealed record GameSnapshot(
    Guid Id,
    string? AdventureId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    PlayerSnapshot Player,
    List<LocationSnapshot> Locations,
    List<ItemSnapshot> Items,
    List<NpcSnapshot> Npcs,
    PuzzleSnapshot Puzzle,
    List<WorldFlagSnapshot> WorldFlags,
    List<GameEventSnapshot> EventHistory,
    string? WorldId = null,
    PlayerKnowledgeSnapshot? PlayerKnowledge = null,
    List<PuzzleSnapshot>? Puzzles = null,
    Guid? AdventureVersionId = null);

public sealed record PlayerSnapshot(string CurrentLocationId, List<string> InventoryItemIds, List<string> KnownClues);

public sealed record KnowledgeEntrySnapshot(string Id, DateTimeOffset DiscoveredAt);

public sealed record PlayerKnowledgeSnapshot(
    List<KnowledgeEntrySnapshot>? Regions = null,
    List<KnowledgeEntrySnapshot>? Locations = null,
    List<KnowledgeEntrySnapshot>? Connections = null,
    List<KnowledgeEntrySnapshot>? Lore = null,
    List<KnowledgeEntrySnapshot>? Clues = null,
    List<KnowledgeEntrySnapshot>? Npcs = null);

public sealed record ExitSnapshot(string Direction, string DestinationId, string? RequiredFlagKey, bool? MustBeSet);

public sealed record LocationSnapshot(
    string Id,
    string Name,
    string BaseDescription,
    List<ExitSnapshot> Exits,
    List<string> ObjectIds,
    List<string> NpcIds,
    bool Discovered);

public sealed record ItemSnapshot(
    string Id,
    string Name,
    string Description,
    int State,
    string? LocationId,
    string? RevealFlagKey,
    bool? RevealMustBeSet);

public sealed record ConversationTurnSnapshot(string PlayerUtterance, string NpcReply, DateTimeOffset Timestamp);

public sealed record NpcSnapshot(
    string Id,
    string Name,
    string PersonalityProfile,
    List<string> KnowledgeBoundary,
    List<ConversationTurnSnapshot> ConversationMemory,
    List<WorldFlagSnapshot> RelationshipFlags,
    string? WorldLocationId = null,
    List<string>? Goals = null,
    List<string>? AllowedLoreReferences = null,
    long StateVersion = 1);

public sealed record PuzzleSnapshot(
    string Id,
    string RequiredItemId,
    string RequiredClueKey,
    bool Solved,
    Puzzles.PuzzleState? State = null,
    List<PuzzlePrerequisiteSnapshot>? Prerequisites = null,
    List<PuzzleSolutionSnapshot>? Solutions = null,
    List<PuzzleOutcomeSnapshot>? Outcomes = null,
    List<string>? ChainLinks = null);

public sealed record PuzzlePrerequisiteSnapshot(Puzzles.PuzzleConditionType Type, string ReferenceId);

public sealed record PuzzleConditionSnapshot(Puzzles.PuzzleConditionType Type, string ReferenceId);

public sealed record PuzzleSolutionSnapshot(string Id, List<PuzzleConditionSnapshot> Conditions);

public sealed record PuzzleOutcomeSnapshot(string Id, Puzzles.PuzzleOutcomeType Type, string ReferenceId);

public sealed record WorldFlagSnapshot(string Key, DateTimeOffset SetAt);

/// <summary>An opaque, Kind-tagged mirror of one GameEvent; Payload keys are Kind-specific (see GameEventSnapshotMapper).</summary>
public sealed record GameEventSnapshot(Guid Id, DateTimeOffset OccurredAt, string Kind, Dictionary<string, string> Payload);
