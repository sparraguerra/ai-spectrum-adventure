namespace AI.SpectrumAdventure.Domain.Worlds;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Items;
using AI.SpectrumAdventure.Domain.Lore;

public sealed class World
{
    private readonly Dictionary<RegionId, Region> _regions;
    private readonly Dictionary<LocationId, WorldLocation> _locations;
    private readonly Dictionary<ConnectionId, WorldConnection> _connections;
    private readonly Dictionary<GenerationKey, GeneratedContentMetadata> _generationMetadata;
    private readonly Dictionary<LoreId, LoreEntry> _loreEntries;
    private readonly Dictionary<ItemId, ItemState> _objectStates = [];
    private readonly Dictionary<NpcId, LocationId> _npcLocations = [];
    private readonly Dictionary<PuzzleId, bool> _puzzleStates = [];
    private readonly List<WorldEvent> _events = [];

    public WorldId Id { get; }
    public WorldSeed Seed { get; }
    public GenerationVersion GenerationVersion { get; }
    public IReadOnlyCollection<Region> Regions => _regions.Values;
    public IReadOnlyCollection<WorldLocation> Locations => _locations.Values;
    public IReadOnlyCollection<WorldConnection> Connections => _connections.Values;
    public IReadOnlyCollection<GeneratedContentMetadata> GenerationMetadata => _generationMetadata.Values;
    public IReadOnlyCollection<LoreEntry> LoreEntries => _loreEntries.Values;
    public IReadOnlyCollection<WorldEvent> Events => _events;
    public IReadOnlyDictionary<ItemId, ItemState> ObjectStates => _objectStates;
    public IReadOnlyDictionary<NpcId, LocationId> NpcLocations => _npcLocations;
    public IReadOnlyDictionary<PuzzleId, bool> PuzzleStates => _puzzleStates;

    public World(WorldId id, WorldSeed seed, GenerationVersion generationVersion, IEnumerable<Region> regions, IEnumerable<WorldLocation> locations, IEnumerable<WorldConnection> connections, IEnumerable<GeneratedContentMetadata>? generationMetadata = null, IEnumerable<WorldEvent>? events = null, IEnumerable<LoreEntry>? loreEntries = null)
    {
        Id = id;
        Seed = seed;
        GenerationVersion = generationVersion;
        _regions = regions?.ToDictionary(region => region.Id) ?? throw new ArgumentNullException(nameof(regions));
        _locations = locations?.ToDictionary(location => location.Id) ?? throw new ArgumentNullException(nameof(locations));
        _connections = connections?.ToDictionary(connection => connection.Id) ?? throw new ArgumentNullException(nameof(connections));
        _generationMetadata = generationMetadata?.ToDictionary(metadata => metadata.GenerationKey) ?? [];
        _loreEntries = loreEntries?.ToDictionary(entry => entry.Id) ?? [];
        ValidateLore();
        if (events is not null)
        {
            _events.AddRange(events.OrderBy(worldEvent => worldEvent.Sequence));
            if (_events.Select(worldEvent => worldEvent.Id).Distinct().Count() != _events.Count || _events.Select(worldEvent => worldEvent.Sequence).SequenceEqual(Enumerable.Range(1, _events.Count).Select(value => (long)value)) is false)
            {
                throw new InvalidOperationException("Rehydrated world events must be unique and monotonic.");
            }

            foreach (var worldEvent in _events.Where(worldEvent => worldEvent is not WorldExpandedEvent))
            {
                ApplyStateChange(worldEvent);
            }
        }
        ValidateGraph();
    }

    public WorldLocation GetLocation(LocationId id) => _locations.TryGetValue(id, out var location) ? location : throw new KeyNotFoundException($"World location '{id}' does not exist.");

    public WorldConnection? FindConnection(LocationId sourceLocationId, ConnectionDirection direction) =>
        _connections.Values.SingleOrDefault(connection => connection.SourceLocationId == sourceLocationId && connection.Direction == direction);

    public bool HasGenerationKey(GenerationKey generationKey) => _generationMetadata.ContainsKey(generationKey);
    public bool HasLore(LoreId loreId) => _loreEntries.ContainsKey(loreId);
    public LoreEntry GetLore(LoreId loreId) => _loreEntries.TryGetValue(loreId, out var entry) ? entry : throw new KeyNotFoundException($"Lore entry '{loreId}' does not exist.");

    public void Apply(WorldEvent worldEvent)
    {
        ArgumentNullException.ThrowIfNull(worldEvent);
        if (worldEvent.Sequence != _events.Count + 1) throw new InvalidOperationException("World event sequence must be monotonic.");
        if (_events.Any(existing => existing.Id == worldEvent.Id)) throw new InvalidOperationException("World events are append-only and unique.");
        ValidateEvent(worldEvent);

        ApplyStateChange(worldEvent);
        _events.Add(worldEvent);
    }

    private void ApplyStateChange(WorldEvent worldEvent)
    {
        switch (worldEvent)
        {
            case WorldExpandedEvent expansion:
                ApplyExpansion(expansion);
                break;
            case ConnectionStateChangedWorldEvent connectionChange:
                GetConnection(connectionChange.ConnectionId).SetState(connectionChange.Visibility, connectionChange.Availability);
                break;
            case LocationStateChangedWorldEvent locationChange:
                GetLocation(locationChange.LocationId).ChangeState(locationChange.State);
                break;
            case ObjectStateChangedWorldEvent objectChange:
                _objectStates[objectChange.ItemId] = objectChange.State;
                break;
            case NpcMovedWorldEvent npcMove:
                GetLocation(npcMove.LocationId);
                _npcLocations[npcMove.NpcId] = npcMove.LocationId;
                break;
            case PuzzleStateChangedWorldEvent puzzleChange:
                _puzzleStates[puzzleChange.PuzzleId] = puzzleChange.IsSolved;
                break;
            default:
                throw new InvalidOperationException($"Unsupported world event type: {worldEvent.GetType().Name}");
        }
    }

    private void ValidateEvent(WorldEvent worldEvent)
    {
        WorldEvent.Validate(worldEvent.Sequence, worldEvent.Cause);
        if (!WorldEvent.IsDeterministicCause(worldEvent.Cause)) throw new InvalidOperationException("World events require a deterministic cause category.");

        switch (worldEvent)
        {
            case ConnectionStateChangedWorldEvent connectionChange:
                var connection = GetConnection(connectionChange.ConnectionId);
                if (connection.Availability == ConnectionAvailability.Blocked && connectionChange.Availability == ConnectionAvailability.Available && !worldEvent.Cause.StartsWith($"{WorldEventCause.PlayerAction}:repair", StringComparison.OrdinalIgnoreCase) && !worldEvent.Cause.StartsWith($"{WorldEventCause.WorldRule}:clear", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("A blocked connection requires an explicit permitted transition to reopen.");
                break;
            case LocationStateChangedWorldEvent locationChange:
                var location = GetLocation(locationChange.LocationId);
                if (location.State == LocationState.Destroyed && locationChange.State != LocationState.Destroyed && !worldEvent.Cause.StartsWith($"{WorldEventCause.PlayerAction}:rebuild", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("A destroyed location requires an explicit permitted transition to restore.");
                break;
        }
    }

    private void ApplyExpansion(WorldExpandedEvent expansion)
    {
        if (expansion.Metadata.WorldSeed != Seed || expansion.Metadata.GenerationVersion != GenerationVersion) throw new InvalidOperationException("Generated content metadata does not belong to this world.");
        if (_generationMetadata.ContainsKey(expansion.Metadata.GenerationKey)) throw new InvalidOperationException("Generation key has already been applied.");
        if (_locations.ContainsKey(expansion.Location.Id) || _connections.ContainsKey(expansion.Connection.Id)) throw new InvalidOperationException("Generated content identity already exists.");
        if (expansion.Connection.DestinationLocationId != expansion.Location.Id || expansion.Connection.SourceLocationId != expansion.Metadata.SourceLocationId || expansion.Connection.Direction != expansion.Metadata.Direction) throw new InvalidOperationException("Expansion connection does not match its generated boundary.");
        if (FindConnection(expansion.Connection.SourceLocationId, expansion.Connection.Direction) is not null) throw new InvalidOperationException("A source direction already has a connection.");

        if (!_regions.TryGetValue(expansion.Location.RegionId, out var region))
        {
            if (expansion.Region is null || expansion.Region.Id != expansion.Location.RegionId) throw new InvalidOperationException("Expansion location must belong to an existing or supplied region.");
            _regions.Add(expansion.Region.Id, expansion.Region);
            region = expansion.Region;
        }

        _locations.Add(expansion.Location.Id, expansion.Location);
        _connections.Add(expansion.Connection.Id, expansion.Connection);
        _generationMetadata.Add(expansion.Metadata.GenerationKey, expansion.Metadata);
        region.AddLocation(expansion.Location.Id);
    }

    private WorldConnection GetConnection(ConnectionId id) => _connections.TryGetValue(id, out var connection) ? connection : throw new KeyNotFoundException($"World connection '{id}' does not exist.");

    private void ValidateGraph()
    {
        foreach (var location in _locations.Values)
        {
            if (!_regions.ContainsKey(location.RegionId)) throw new InvalidOperationException($"Location '{location.Id}' references an unknown region.");
        }

        foreach (var connection in _connections.Values)
        {
            if (!_locations.ContainsKey(connection.SourceLocationId) || !_locations.ContainsKey(connection.DestinationLocationId)) throw new InvalidOperationException($"Connection '{connection.Id}' references an unknown location.");
        }

        if (_connections.Values.GroupBy(connection => (connection.SourceLocationId, connection.Direction)).Any(group => group.Count() > 1)) throw new InvalidOperationException("A location can have only one connection in each direction.");
    }

    private void ValidateLore()
    {
        foreach (var lore in _loreEntries.Values)
        {
            if (lore.Scope == LoreScope.Regional && lore.SubjectReferences.Any(reference => !_regions.ContainsKey(new RegionId(reference)))) throw new InvalidOperationException($"Regional lore '{lore.Id}' references an unknown region.");
            if (lore.Scope == LoreScope.Location && lore.SubjectReferences.Any(reference => !_locations.ContainsKey(new LocationId(reference)))) throw new InvalidOperationException($"Location lore '{lore.Id}' references an unknown location.");
        }

        if (_loreEntries.Values.Any(left => _loreEntries.Values.Any(right => left.Id != right.Id && left.Contradicts(right)))) throw new InvalidOperationException("Immutable or historical lore cannot be contradictory.");
    }
}