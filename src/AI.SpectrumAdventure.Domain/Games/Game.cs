namespace AI.SpectrumAdventure.Domain.Games;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Items;
using AI.SpectrumAdventure.Domain.Locations;
using AI.SpectrumAdventure.Domain.Npcs;
using AI.SpectrumAdventure.Domain.Players;
using AI.SpectrumAdventure.Domain.Puzzles;

/// <summary>The authoritative game aggregate root (constitution Principle I). All mutation flows through Apply(GameEvent).</summary>
public sealed class Game
{
    private readonly Dictionary<LocationId, Location> _locations;
    private readonly Dictionary<ItemId, GameItem> _items;
    private readonly Dictionary<NpcId, Npc> _npcs;
    private readonly HashSet<string> _worldFlagKeys = [];
    private readonly List<WorldFlag> _worldFlagHistory = [];
    private readonly List<GameEvent> _eventHistory = [];

    public GameId Id { get; }
    public string AdventureId { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Player Player { get; }
    public Puzzle Puzzle { get; }

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
        string adventureId = AdventureWorldFactory.DefaultAdventureId)
    {
        Id = id;
        AdventureId = adventureId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Player = player;
        Puzzle = puzzle;
        _locations = locations.ToDictionary(l => l.Id);
        _items = items.ToDictionary(i => i.Id);
        _npcs = npcs.ToDictionary(n => n.Id);
    }

    public Location GetLocation(LocationId id) => _locations[id];

    public GameItem GetItem(ItemId id) => _items[id];

    public Npc GetNpc(NpcId id) => _npcs[id];

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

            case PuzzleSolvedEvent:
                Puzzle.MarkSolved();
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
            [.. n.RelationshipFlags.Select(f => new WorldFlagSnapshot(f.Key, f.SetAt))]))],
        new PuzzleSnapshot(Puzzle.Id.Value, Puzzle.RequiredItemId.Value, Puzzle.RequiredClueKey, Puzzle.Solved),
        [.. ActiveWorldFlags.Select(f => new WorldFlagSnapshot(f.Key, f.SetAt))],
        [.. EventHistory.Select(GameEventSnapshotMapper.ToSnapshot)]);

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
            var npc = new Npc(new NpcId(ns.Id), ns.Name, ns.PersonalityProfile, ns.KnowledgeBoundary);
            foreach (var turn in ns.ConversationMemory)
            {
                npc.RecordConversationTurn(turn.PlayerUtterance, turn.NpcReply, turn.Timestamp);
            }

            foreach (var flag in ns.RelationshipFlags)
            {
                npc.RecordRelationshipFlag(new WorldFlag(flag.Key, flag.SetAt));
            }

            return npc;
        }).ToList();

        var puzzle = new Puzzle(new PuzzleId(snapshot.Puzzle.Id), new PuzzleSolutionCondition(new ItemId(snapshot.Puzzle.RequiredItemId), snapshot.Puzzle.RequiredClueKey));
        if (snapshot.Puzzle.Solved)
        {
            puzzle.MarkSolved();
        }

        var player = new Player(new LocationId(snapshot.Player.CurrentLocationId));
        foreach (var itemId in snapshot.Player.InventoryItemIds)
        {
            player.Inventory.Add(new ItemId(itemId));
        }

        foreach (var clue in snapshot.Player.KnownClues)
        {
            player.LearnClue(clue);
        }

        var game = new Game(new GameId(snapshot.Id), snapshot.CreatedAt, player, locations, items, npcs, puzzle, snapshot.AdventureId ?? AdventureWorldFactory.DefaultAdventureId);
        game.RehydrateWorldFlagsAndHistory(
            snapshot.WorldFlags.Select(f => new WorldFlag(f.Key, f.SetAt)),
            snapshot.EventHistory.Select(GameEventSnapshotMapper.FromSnapshot),
            snapshot.UpdatedAt);

        return game;
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
