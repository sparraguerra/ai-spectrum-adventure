namespace AI.SpectrumAdventure.Domain.Games;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Items;
using AI.SpectrumAdventure.Domain.Locations;
using AI.SpectrumAdventure.Domain.Npcs;
using AI.SpectrumAdventure.Domain.Players;
using AI.SpectrumAdventure.Domain.Puzzles;
using AI.SpectrumAdventure.Domain.Worlds;

/// <summary>The authoritative game aggregate root (constitution Principle I). All mutation flows through Apply(GameEvent).</summary>
public sealed class Game
{
    private readonly Dictionary<LocationId, Location> _locations;
    private readonly Dictionary<ItemId, GameItem> _items;
    private readonly Dictionary<NpcId, Npc> _npcs;
    private readonly Dictionary<PuzzleId, Puzzle> _puzzles;
    private readonly HashSet<string> _worldFlagKeys = [];
    private readonly List<WorldFlag> _worldFlagHistory = [];
    private readonly List<GameEvent> _eventHistory = [];

    public GameId Id { get; }
    public WorldId? WorldId { get; private set; }
    public string AdventureId { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Player Player { get; }
    public Puzzle Puzzle { get; }
    public IReadOnlyCollection<Puzzle> Puzzles => _puzzles.Values;
    public PlayerKnowledge PlayerKnowledge { get; }

    public IReadOnlyCollection<Location> Locations => _locations.Values;
    public IReadOnlyCollection<GameItem> Items => _items.Values;
    public IReadOnlyCollection<Npc> Npcs => _npcs.Values;
    public IReadOnlySet<string> WorldFlagKeys => _worldFlagKeys;
    public IReadOnlyCollection<WorldFlag> ActiveWorldFlags => _worldFlagHistory;

    /// <summary>Append-only log of every event ever applied to this game (FR-003/FR-026, SC-008).</summary>
    public IReadOnlyCollection<GameEvent> EventHistory => _eventHistory;

    public Location CurrentLocation => _locations[Player.CurrentLocationId];

    public Game(
        GameId id,
        DateTimeOffset createdAt,
        Player player,
        IEnumerable<Location> locations,
        IEnumerable<GameItem> items,
        IEnumerable<Npc> npcs,
        Puzzle puzzle,
        string adventureId = AdventureWorldFactory.DefaultAdventureId,
        WorldId? worldId = null,
        PlayerKnowledge? playerKnowledge = null,
        IEnumerable<Puzzle>? puzzles = null)
    {
        Id = id;
        WorldId = worldId;
        AdventureId = adventureId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Player = player;
        Puzzle = puzzle;
        _puzzles = (puzzles ?? [puzzle]).ToDictionary(candidate => candidate.Id);
        if (!_puzzles.ContainsKey(puzzle.Id)) throw new ArgumentException("The primary puzzle must be included in the puzzle collection.", nameof(puzzles));
        PlayerKnowledge = playerKnowledge ?? new PlayerKnowledge();
        _locations = locations.ToDictionary(l => l.Id);
        _items = items.ToDictionary(i => i.Id);
        _npcs = npcs.ToDictionary(n => n.Id);
    }

    public Location GetLocation(LocationId id) => _locations[id];

    public GameItem GetItem(ItemId id) => _items[id];

    public void BindWorld(WorldId worldId)
    {
        if (WorldId is not null && WorldId != worldId) throw new InvalidOperationException("A game cannot be bound to a different world.");
        WorldId = worldId;
    }

    public void AddGeneratedLocation(WorldLocation location, WorldConnection connection)
    {
        if (connection.DestinationLocationId != location.Id || !_locations.ContainsKey(connection.SourceLocationId)) throw new InvalidOperationException("Generated connection does not belong to this game.");
        if (!_locations.ContainsKey(location.Id))
        {
            _locations.Add(location.Id, new Location(location.Id, location.StructuralNameKey.Replace('-', ' '), location.BaseDescription));
        }
        _locations[connection.SourceLocationId].AddExit(new Exit(connection.Direction.ToString(), location.Id));
    }

    public Npc GetNpc(NpcId id) => _npcs[id];

    public Puzzle GetPuzzle(PuzzleId id) => _puzzles.TryGetValue(id, out var puzzle) ? puzzle : throw new KeyNotFoundException($"Puzzle '{id}' does not exist.");

    public bool HasFlag(string key) => _worldFlagKeys.Contains(key);

    /// <summary>
    /// Applies a single already-validated GameEvent. Only the AdventureOrchestrator (after RulesEngine validation)
    /// may call this; agents never construct or apply GameEvents directly (constitution Principles III/IV).
    /// </summary>
    public void Apply(GameEvent gameEvent)
    {
        switch (gameEvent)
        {
            case PlayerMovedEvent e:
                Player.MoveTo(e.ToLocationId);
                break;

            case ItemTakenEvent e:
                GetLocation(e.LocationId).RemoveItem(e.ItemId);
                GetItem(e.ItemId).MoveTo(null);
                Player.Inventory.Add(e.ItemId);
                break;

            case ItemUsedEvent:
                // Item-specific consequences (e.g., a puzzle solution) are expressed via their own dedicated events
                // (e.g., PuzzleSolvedEvent) applied alongside this one; this event itself only records the usage.
                break;

            case ObjectStateChangedEvent e:
                GetItem(e.ItemId).SetState(e.NewState);
                break;

            case DoorOpenedEvent:
                // Reserved for future non-puzzle doors; the MVP's only door (Forgotten Tower) unlocks via PuzzleSolvedEvent.
                break;

            case NpcRelationshipChangedEvent e:
                ApplyNpcRelationshipChanged(e);
                break;

            case PuzzleSolvedEvent e:
                GetPuzzle(e.PuzzleId).MarkSolved();
                SetFlag(WorldFlags.TowerUnlocked, gameEvent.OccurredAt);
                break;

            case LocationDiscoveredEvent e:
                GetLocation(e.LocationId).MarkDiscovered();
                break;

            default:
                throw new InvalidOperationException($"Unknown game event type: {gameEvent.GetType().Name}");
        }

        _eventHistory.Add(gameEvent);
        UpdatedAt = gameEvent.OccurredAt;
    }

    /// <summary>Applies a sequence of pre-validated events. Events are only ever produced by the RulesEngine after
    /// a successful validation, so a mid-sequence failure indicates a programming error, not an invalid player action.</summary>
    public void ApplyRange(IEnumerable<GameEvent> events)
    {
        foreach (var gameEvent in events)
        {
            Apply(gameEvent);
        }
    }

    private void ApplyNpcRelationshipChanged(NpcRelationshipChangedEvent e)
    {
        var npc = GetNpc(e.NpcId);
        npc.RecordRelationshipFlag(new WorldFlag(e.FlagKey, e.OccurredAt));

        if (e.PlayerUtterance is not null && e.NpcReply is not null)
        {
            npc.RecordConversationTurn(e.PlayerUtterance, e.NpcReply, e.OccurredAt);
        }

        if (e.FlagKey == WorldFlags.NpcGaveClue)
        {
            Player.LearnClue(Puzzle.RequiredClueKey);
        }

        SetFlag(e.FlagKey, e.OccurredAt);
    }

    private void SetFlag(string key, DateTimeOffset at)
    {
        if (_worldFlagKeys.Add(key))
        {
            _worldFlagHistory.Add(new WorldFlag(key, at));
        }
    }

    /// <summary>Persistence-boundary export (Infrastructure serializes this to JSON; see research.md Decision 1).</summary>
    public GameSnapshot ToSnapshot() => new(
        Id.Value,
        AdventureId,
        CreatedAt,
        UpdatedAt,
        new PlayerSnapshot(
            Player.CurrentLocationId.Value,
            [.. Player.Inventory.Items.Select(i => i.Value)],
            [.. Player.KnownClues]),
        [.. Locations.Select(l => new LocationSnapshot(
            l.Id.Value,
            l.Name,
            l.BaseDescription,
            [.. l.Exits.Select(e => new ExitSnapshot(e.Direction, e.DestinationId.Value, e.RequiredCondition?.RequiredFlagKey, e.RequiredCondition?.MustBeSet))],
            [.. l.ObjectIds.Select(i => i.Value)],
            [.. l.NpcIds.Select(n => n.Value)],
            l.Discovered))],
        [.. Items.Select(i => new ItemSnapshot(
            i.Id.Value, i.Name, i.Description, (int)i.State, i.LocationId?.Value, i.RevealCondition?.RequiredFlagKey, i.RevealCondition?.MustBeSet))],
        [.. Npcs.Select(n => new NpcSnapshot(
            n.Id.Value,
            n.Name,
            n.PersonalityProfile,
            [.. n.KnowledgeBoundary],
            [.. n.ConversationMemory.Select(c => new ConversationTurnSnapshot(c.PlayerUtterance, c.NpcReply, c.Timestamp))],
            [.. n.RelationshipFlags.Select(f => new WorldFlagSnapshot(f.Key, f.SetAt))],
            n.WorldLocationId?.Value,
            [.. n.Goals],
            [.. n.AllowedLoreReferences],
            n.StateVersion))],
        ToSnapshot(Puzzle),
        [.. ActiveWorldFlags.Select(f => new WorldFlagSnapshot(f.Key, f.SetAt))],
        [.. EventHistory.Select(GameEventSnapshotMapper.ToSnapshot)],
        WorldId?.Value.ToString(),
        PlayerKnowledge.ToSnapshot(),
        [.. Puzzles.Select(ToSnapshot)]);

    /// <summary>Persistence-boundary import: reconstructs a full Game aggregate from a previously exported snapshot.</summary>
    public static Game FromSnapshot(GameSnapshot snapshot)
    {
        var locations = snapshot.Locations.Select(ls => new Location(
            new LocationId(ls.Id),
            ls.Name,
            ls.BaseDescription,
            exits: ls.Exits.Select(es => new Exit(
                es.Direction,
                new LocationId(es.DestinationId),
                es.RequiredFlagKey is null ? null : new WorldFlagCondition(es.RequiredFlagKey, es.MustBeSet ?? true))),
            objectIds: ls.ObjectIds.Select(id => new ItemId(id)),
            npcIds: ls.NpcIds.Select(id => new NpcId(id)))).ToList();

        foreach (var (locationSnapshot, location) in snapshot.Locations.Zip(locations))
        {
            if (locationSnapshot.Discovered)
            {
                location.MarkDiscovered();
            }
        }

        var items = snapshot.Items.Select(its => new GameItem(
            new ItemId(its.Id),
            its.Name,
            its.Description,
            (ItemState)its.State,
            its.LocationId is null ? null : new LocationId(its.LocationId),
            its.RevealFlagKey is null ? null : new WorldFlagCondition(its.RevealFlagKey, its.RevealMustBeSet ?? true))).ToList();

        var npcs = snapshot.Npcs.Select(ns =>
        {
            var npc = new Npc(
                new NpcId(ns.Id), ns.Name, ns.PersonalityProfile, ns.KnowledgeBoundary,
                ns.WorldLocationId is null ? null : new LocationId(ns.WorldLocationId),
                ns.Goals, ns.AllowedLoreReferences, ns.StateVersion);
            foreach (var turn in ns.ConversationMemory)
            {
                npc.RecordConversationTurn(turn.PlayerUtterance, turn.NpcReply, turn.Timestamp);
            }

            foreach (var flag in ns.RelationshipFlags)
            {
                npc.RehydrateRelationshipFlag(new WorldFlag(flag.Key, flag.SetAt));
            }

            return npc;
        }).ToList();

        var puzzleSnapshots = snapshot.Puzzles is { Count: > 0 } ? snapshot.Puzzles : [snapshot.Puzzle];
        var puzzles = puzzleSnapshots.Select(FromSnapshot).ToList();
        var puzzle = puzzles.Single(candidate => candidate.Id.Value == snapshot.Puzzle.Id);

        var player = new Player(new LocationId(snapshot.Player.CurrentLocationId));
        foreach (var itemId in snapshot.Player.InventoryItemIds)
        {
            player.Inventory.Add(new ItemId(itemId));
        }

        foreach (var clue in snapshot.Player.KnownClues)
        {
            player.LearnClue(clue);
        }

        WorldId? worldId = Guid.TryParse(snapshot.WorldId, out var worldGuid) ? new WorldId(worldGuid) : null;
        var game = new Game(new GameId(snapshot.Id), snapshot.CreatedAt, player, locations, items, npcs, puzzle, snapshot.AdventureId ?? AdventureWorldFactory.DefaultAdventureId, worldId, PlayerKnowledge.FromSnapshot(snapshot.PlayerKnowledge), puzzles);
        game.RehydrateWorldFlagsAndHistory(
            snapshot.WorldFlags.Select(f => new WorldFlag(f.Key, f.SetAt)),
            snapshot.EventHistory.Select(GameEventSnapshotMapper.FromSnapshot),
            snapshot.UpdatedAt);

        return game;
    }

    private static PuzzleSnapshot ToSnapshot(Puzzle puzzle) => new(
        puzzle.Id.Value,
        puzzle.RequiredItemId.Value,
        puzzle.RequiredClueKey,
        puzzle.Solved,
        puzzle.State,
        [.. puzzle.Prerequisites.Select(prerequisite => new PuzzlePrerequisiteSnapshot(prerequisite.Type, prerequisite.ReferenceId))],
        [.. puzzle.Solutions.Select(solution => new PuzzleSolutionSnapshot(solution.Id, [.. solution.Conditions.Select(condition => new PuzzleConditionSnapshot(condition.Type, condition.ReferenceId))]))],
        [.. puzzle.Outcomes.Select(outcome => new PuzzleOutcomeSnapshot(outcome.Id, outcome.Type, outcome.ReferenceId))],
        [.. puzzle.ChainLinks.Select(link => link.NextPuzzleId.Value)]);

    private static Puzzle FromSnapshot(PuzzleSnapshot snapshot)
    {
        var puzzle = snapshot.Solutions is { Count: > 0 }
            ? new Puzzle(
                new PuzzleId(snapshot.Id),
                snapshot.Solutions.Select(solution => new PuzzleSolutionDefinition(solution.Id, solution.Conditions.Select(condition => new PuzzleCondition(condition.Type, condition.ReferenceId)).ToArray())),
                snapshot.Prerequisites?.Select(prerequisite => new PuzzlePrerequisiteReference(prerequisite.Type, prerequisite.ReferenceId)),
                snapshot.Outcomes?.Select(outcome => new PuzzleOutcomeDefinition(outcome.Id, outcome.Type, outcome.ReferenceId)),
                snapshot.ChainLinks?.Select(puzzleId => new PuzzleChainLink(new PuzzleId(puzzleId))),
                snapshot.State ?? PuzzleState.Discovered)
            : new Puzzle(new PuzzleId(snapshot.Id), new PuzzleSolutionCondition(new ItemId(snapshot.RequiredItemId), snapshot.RequiredClueKey));
        if (snapshot.Solved && !puzzle.Solved) puzzle.MarkSolved();
        return puzzle;
    }

    /// <summary>Restores already-established world flags and event history without re-running Apply's business logic
    /// (the corresponding state effects were already captured directly from the snapshot's other fields).</summary>
    private void RehydrateWorldFlagsAndHistory(IEnumerable<WorldFlag> worldFlags, IEnumerable<GameEvent> eventHistory, DateTimeOffset updatedAt)
    {
        foreach (var flag in worldFlags)
        {
            if (_worldFlagKeys.Add(flag.Key))
            {
                _worldFlagHistory.Add(flag);
            }
        }

        _eventHistory.AddRange(eventHistory);
        UpdatedAt = updatedAt;
    }
}

public sealed class PlayerKnowledge
{
    private readonly Dictionary<string, DateTimeOffset> _regions = [];
    private readonly Dictionary<string, DateTimeOffset> _locations = [];
    private readonly Dictionary<string, DateTimeOffset> _connections = [];
    private readonly Dictionary<string, DateTimeOffset> _lore = [];
    private readonly Dictionary<string, DateTimeOffset> _clues = [];
    private readonly Dictionary<string, DateTimeOffset> _npcs = [];

    public IReadOnlyDictionary<string, DateTimeOffset> Regions => _regions;
    public IReadOnlyDictionary<string, DateTimeOffset> Locations => _locations;
    public IReadOnlyDictionary<string, DateTimeOffset> Connections => _connections;
    public IReadOnlyDictionary<string, DateTimeOffset> Lore => _lore;
    public IReadOnlyDictionary<string, DateTimeOffset> Clues => _clues;
    public IReadOnlyDictionary<string, DateTimeOffset> Npcs => _npcs;

    public bool DiscoverRegion(RegionId id, DateTimeOffset at) => Discover(_regions, id.Value, at);
    public bool DiscoverLocation(LocationId id, DateTimeOffset at) => Discover(_locations, id.Value, at);
    public bool DiscoverConnection(ConnectionId id, DateTimeOffset at) => Discover(_connections, id.Value, at);
    public bool DiscoverLore(LoreId id, DateTimeOffset at) => Discover(_lore, id.Value, at);
    public bool DiscoverClue(string id, DateTimeOffset at) => Discover(_clues, id, at);
    public bool DiscoverNpc(NpcId id, DateTimeOffset at) => Discover(_npcs, id.Value, at);

    internal PlayerKnowledgeSnapshot ToSnapshot() => new(ToEntries(_regions), ToEntries(_locations), ToEntries(_connections), ToEntries(_lore), ToEntries(_clues), ToEntries(_npcs));

    internal static PlayerKnowledge FromSnapshot(PlayerKnowledgeSnapshot? snapshot)
    {
        var knowledge = new PlayerKnowledge();
        if (snapshot is null) return knowledge;
        Restore(knowledge._regions, snapshot.Regions);
        Restore(knowledge._locations, snapshot.Locations);
        Restore(knowledge._connections, snapshot.Connections);
        Restore(knowledge._lore, snapshot.Lore);
        Restore(knowledge._clues, snapshot.Clues);
        Restore(knowledge._npcs, snapshot.Npcs);
        return knowledge;
    }

    private static bool Discover(Dictionary<string, DateTimeOffset> entries, string id, DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A discovered identifier is required.", nameof(id));
        return entries.TryAdd(id, at);
    }

    private static List<KnowledgeEntrySnapshot> ToEntries(Dictionary<string, DateTimeOffset> entries) => [.. entries.Select(entry => new KnowledgeEntrySnapshot(entry.Key, entry.Value))];
    private static void Restore(Dictionary<string, DateTimeOffset> entries, IEnumerable<KnowledgeEntrySnapshot>? values)
    {
        if (values is not null) foreach (var value in values) Discover(entries, value.Id, value.DiscoveredAt);
    }
}
