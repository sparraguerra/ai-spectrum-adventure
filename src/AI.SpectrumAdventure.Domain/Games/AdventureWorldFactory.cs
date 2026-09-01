namespace AI.SpectrumAdventure.Domain.Games;

using System.Text.Json;
using System.Text.Json.Serialization;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Items;
using AI.SpectrumAdventure.Domain.Locations;
using AI.SpectrumAdventure.Domain.Npcs;
using AI.SpectrumAdventure.Domain.Players;
using AI.SpectrumAdventure.Domain.Puzzles;

/// <summary>
/// Loads adventure definitions into the deterministic game model.
/// </summary>
public static class AdventureWorldFactory
{
    public const string DefaultAdventureId = "forgotten-tower";

    public static readonly LocationId ForestEntrance = new("forest-entrance");
    public static readonly LocationId DarkForest = new("dark-forest");
    public static readonly LocationId OldBridge = new("old-bridge");
    public static readonly LocationId ForgottenTower = new("forgotten-tower");

    public static readonly ItemId Sign = new("sign");
    public static readonly ItemId BridgeKey = new("bridge-key");

    public static readonly NpcId Hermit = new("hermit");
    public static readonly PuzzleId ForgottenTowerEntrance = new("forgotten-tower-entrance");

    /// <summary>Knowledge/clue topic key the Hermit may reveal; also Puzzle.RequiredClueKey.</summary>
    public const string TowerClueKey = "tower-clue";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static Game CreateNewGame(GameId id, DateTimeOffset createdAt) =>
        CreateNewGame(id, createdAt, DefaultAdventureId);

    public static Game CreateNewGame(GameId id, DateTimeOffset createdAt, string adventureId) =>
        CreateFromDefinition(id, createdAt, LoadDefinition(adventureId));

    public static Game CreateNewGameFromJson(GameId id, DateTimeOffset createdAt, string json)
    {
        var definition = JsonSerializer.Deserialize<AdventureDefinition>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Adventure JSON is empty or invalid.");

        return CreateFromDefinition(id, createdAt, definition);
    }

    private static AdventureDefinition LoadDefinition(string adventureId)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Adventures", $"{adventureId}.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Adventure definition '{adventureId}' was not found.", path);
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AdventureDefinition>(json, SerializerOptions)
            ?? throw new InvalidOperationException($"Adventure definition '{adventureId}' is empty or invalid.");
    }

    private static Game CreateFromDefinition(GameId id, DateTimeOffset createdAt, AdventureDefinition definition)
    {
        Validate(definition);

        var itemLocations = definition.Locations
            .SelectMany(location => location.ObjectIds.Select(itemId => new { ItemId = itemId, LocationId = location.Id }))
            .ToDictionary(pair => pair.ItemId, pair => pair.LocationId, StringComparer.OrdinalIgnoreCase);

        var items = definition.Items.Select(item => new GameItem(
            new ItemId(item.Id),
            item.Name,
            item.Description,
            ParseItemState(item.States),
            itemLocations.TryGetValue(item.Id, out var locationId) ? new LocationId(locationId) : null,
            item.RevealCondition is null ? null : new WorldFlagCondition(item.RevealCondition.RequiredFlagKey, item.RevealCondition.MustBeSet))).ToList();

        var locations = definition.Locations.Select(location => new Location(
            new LocationId(location.Id),
            location.Name,
            location.Description,
            exits: location.Exits.Select(exit => new Exit(
                exit.Direction,
                new LocationId(exit.To),
                exit.RequiredFlagKey is null ? null : new WorldFlagCondition(exit.RequiredFlagKey, exit.MustBeSet))),
            objectIds: location.ObjectIds.Select(objectId => new ItemId(objectId)),
            npcIds: location.NpcIds.Select(npcId => new NpcId(npcId)))).ToList();

        var npcs = definition.Npcs.Select(npc => new Npc(
            new NpcId(npc.Id),
            npc.Name,
            npc.Personality,
            npc.KnowledgeBoundary)).ToList();

        var puzzle = new Puzzle(
            new PuzzleId(definition.Puzzle.Id),
            new PuzzleSolutionCondition(new ItemId(definition.Puzzle.RequiredItemId), definition.Puzzle.RequiredClueKey));

        var player = new Player(new LocationId(definition.StartingLocationId));

        return new Game(
            id,
            createdAt,
            player,
            locations,
            items,
            npcs,
            puzzle,
            definition.Id);
    }

    private static ItemState ParseItemState(IEnumerable<string> states)
    {
        var result = ItemState.None;
        foreach (var state in states)
        {
            result |= Enum.Parse<ItemState>(state, ignoreCase: true);
        }

        return result;
    }

    private static void Validate(AdventureDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.StartingLocationId))
        {
            throw new InvalidOperationException("Adventure must define startingLocationId.");
        }

        var locationIds = definition.Locations.Select(location => location.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var itemIds = definition.Items.Select(item => item.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var npcIds = definition.Npcs.Select(npc => npc.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!locationIds.Contains(definition.StartingLocationId))
        {
            throw new InvalidOperationException($"Starting location '{definition.StartingLocationId}' does not exist.");
        }

        foreach (var location in definition.Locations)
        {
            foreach (var exit in location.Exits.Where(exit => !locationIds.Contains(exit.To)))
            {
                throw new InvalidOperationException($"Location '{location.Id}' has an exit to unknown location '{exit.To}'.");
            }

            foreach (var objectId in location.ObjectIds.Where(objectId => !itemIds.Contains(objectId)))
            {
                throw new InvalidOperationException($"Location '{location.Id}' references unknown item '{objectId}'.");
            }

            foreach (var npcId in location.NpcIds.Where(npcId => !npcIds.Contains(npcId)))
            {
                throw new InvalidOperationException($"Location '{location.Id}' references unknown NPC '{npcId}'.");
            }
        }

        if (!itemIds.Contains(definition.Puzzle.RequiredItemId))
        {
            throw new InvalidOperationException($"Puzzle '{definition.Puzzle.Id}' requires unknown item '{definition.Puzzle.RequiredItemId}'.");
        }
    }

    private sealed class AdventureDefinition
    {
        public string Id { get; init; } = "";
        public string Title { get; init; } = "";
        public string StartingLocationId { get; init; } = "";
        public List<LocationDefinition> Locations { get; init; } = [];
        public List<ItemDefinition> Items { get; init; } = [];
        public List<NpcDefinition> Npcs { get; init; } = [];
        public PuzzleDefinition Puzzle { get; init; } = new();
    }

    private sealed class LocationDefinition
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public List<ExitDefinition> Exits { get; init; } = [];
        public List<string> ObjectIds { get; init; } = [];
        public List<string> NpcIds { get; init; } = [];
    }

    private sealed class ExitDefinition
    {
        public string Direction { get; init; } = "";
        public string To { get; init; } = "";
        public string? RequiredFlagKey { get; init; }
        public bool MustBeSet { get; init; } = true;
    }

    private sealed class ItemDefinition
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public List<string> States { get; init; } = [];
        public RevealConditionDefinition? RevealCondition { get; init; }
    }

    private sealed class RevealConditionDefinition
    {
        public string RequiredFlagKey { get; init; } = "";
        public bool MustBeSet { get; init; } = true;
    }

    private sealed class NpcDefinition
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Personality { get; init; } = "";
        public List<string> KnowledgeBoundary { get; init; } = [];
    }

    private sealed class PuzzleDefinition
    {
        public string Id { get; init; } = "";
        public string RequiredItemId { get; init; } = "";
        public string RequiredClueKey { get; init; } = "";
    }
}
