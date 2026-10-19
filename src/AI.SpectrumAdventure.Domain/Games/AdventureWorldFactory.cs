namespace AI.SpectrumAdventure.Domain.Games;

using System.Text.Json;
using System.Text.Json.Serialization;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Items;
using AI.SpectrumAdventure.Domain.Locations;
using AI.SpectrumAdventure.Domain.Lore;
using AI.SpectrumAdventure.Domain.Npcs;
using AI.SpectrumAdventure.Domain.Players;
using AI.SpectrumAdventure.Domain.Puzzles;
using AI.SpectrumAdventure.Domain.Worlds;

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

    public static Game CreateNewGame(GameId id, DateTimeOffset createdAt, AI.SpectrumAdventure.Domain.Authoring.AdventureVersionId? adventureVersionId) =>
        CreateNewGame(id, createdAt, DefaultAdventureId, adventureVersionId);

    public static Game CreateNewGame(GameId id, DateTimeOffset createdAt, string adventureId, AI.SpectrumAdventure.Domain.Authoring.AdventureVersionId? adventureVersionId = null) =>
        CreateFromDefinition(id, createdAt, LoadDefinition(adventureId), adventureVersionId);

    public static Game CreateNewGameFromJson(GameId id, DateTimeOffset createdAt, string json, AI.SpectrumAdventure.Domain.Authoring.AdventureVersionId? adventureVersionId = null)
    {
        var definition = JsonSerializer.Deserialize<AdventureDefinition>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Adventure JSON is empty or invalid.");

        return CreateFromDefinition(id, createdAt, definition, adventureVersionId);
    }

    public static IReadOnlyList<LoreEntry> CreateLoreEntriesFromJson(string json)
    {
        var definition = JsonSerializer.Deserialize<AdventureDefinition>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Adventure JSON is empty or invalid.");

        return definition.Lore.Select(lore =>
        {
            var classification = lore.Classification;
            var category = Enum.TryParse<LoreCategory>(lore.Category, true, out var parsedCategory)
                ? parsedCategory
                : classification switch
                {
                    "RegionalFact" => LoreCategory.Geography,
                    "Rumour" => LoreCategory.Rumour,
                    "PlayerDiscoverableKnowledge" => LoreCategory.Event,
                    _ => LoreCategory.History
                };
            var scope = Enum.TryParse<LoreScope>(lore.Scope, true, out var parsedScope)
                ? parsedScope
                : string.Equals(classification, "RegionalFact", StringComparison.OrdinalIgnoreCase)
                    ? LoreScope.Regional
                    : LoreScope.World;
            var truth = classification switch
            {
                "ImmutableFact" => LoreTruthClassification.ImmutableFact,
                "HistoricalFact" => LoreTruthClassification.HistoricalFact,
                "Rumour" => LoreTruthClassification.Rumour,
                _ => LoreTruthClassification.CurrentFact
            };
            var subjectReferences = lore.SubjectReferences;
            if (scope == LoreScope.Regional && subjectReferences.Count == 0)
                subjectReferences = [$"{definition.Id}-initial"];

            return new LoreEntry(
                new LoreId(lore.Id), category, scope, truth, lore.ContentKey, lore.Content,
                subjectReferences,
                lore.IsProtected,
                string.Equals(classification, "PlayerDiscoverableKnowledge", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(classification, "RegionalFact", StringComparison.OrdinalIgnoreCase));
        }).ToArray();
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

    private static Game CreateFromDefinition(GameId id, DateTimeOffset createdAt, AdventureDefinition definition, AI.SpectrumAdventure.Domain.Authoring.AdventureVersionId? adventureVersionId = null)
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

        var npcLocations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var location in definition.Locations)
        {
            foreach (var npcId in location.NpcIds)
            {
                npcLocations.TryAdd(npcId, location.Id);
            }
        }
        foreach (var npc in definition.Npcs.Where(npc => !string.IsNullOrWhiteSpace(npc.LocationId)))
            npcLocations.TryAdd(npc.Id, npc.LocationId!);

        var locations = definition.Locations.Select(location => new Location(
            new LocationId(location.Id),
            location.Name,
            location.Description,
            exits: location.Exits.Select(exit => new Exit(
                exit.Direction,
                new LocationId(exit.To),
                exit.RequiredFlagKey is null ? null : new WorldFlagCondition(exit.RequiredFlagKey, exit.MustBeSet))),
            objectIds: location.ObjectIds.Select(objectId => new ItemId(objectId)),
            npcIds: npcLocations.Where(pair => string.Equals(pair.Value, location.Id, StringComparison.OrdinalIgnoreCase))
                .Select(pair => new NpcId(pair.Key)),
            type: location.Type,
            environment: location.Environment)).ToList();

        var npcs = definition.Npcs.Select(npc => new Npc(
            new NpcId(npc.Id),
            npc.Name,
            npc.Personality,
            npc.KnowledgeBoundary,
            npcLocations.TryGetValue(npc.Id, out var locationId) ? new LocationId(locationId) : null,
            goals: npc.Goals,
            relationships: npc.Relationships,
            allowedLoreReferences: npc.KnowledgeBoundary)).ToList();

        var puzzles = GetPuzzleDefinitions(definition).Select(CreatePuzzle).ToList();
        var puzzle = puzzles[0];

        var player = new Player(new LocationId(definition.StartingLocationId));

        return new Game(
            id,
            createdAt,
            player,
            locations,
            items,
            npcs,
            puzzle,
            definition.Id,
            puzzles: puzzles,
            adventureVersionId: adventureVersionId,
            targetVocabulary: definition.TargetVocabulary.SelectMany(target => target.Synonyms.Append(target.Id).Select(synonym => new KeyValuePair<string, string>(synonym, target.Id))));
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
        var puzzleIds = GetPuzzleDefinitions(definition).Select(puzzle => puzzle.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var targetIds = itemIds.Concat(npcIds).Concat(puzzleIds).Concat(locationIds).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!locationIds.Contains(definition.StartingLocationId))
        {
            throw new InvalidOperationException($"Starting location '{definition.StartingLocationId}' does not exist.");
        }

        foreach (var target in definition.TargetVocabulary)
        {
            if (!targetIds.Contains(target.Id))
                throw new InvalidOperationException($"Target vocabulary references unknown target '{target.Id}'.");

            if (target.Synonyms.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException($"Target vocabulary for '{target.Id}' contains an empty synonym.");
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

        foreach (var npc in definition.Npcs)
        {
            if (npc.LocationId is not null && !locationIds.Contains(npc.LocationId))
                throw new InvalidOperationException($"NPC '{npc.Id}' references unknown location '{npc.LocationId}'.");

            var placements = definition.Locations
                .Where(location => location.NpcIds.Contains(npc.Id, StringComparer.OrdinalIgnoreCase))
                .Select(location => location.Id)
                .ToArray();
            if (placements.Length > 1 || npc.LocationId is not null && placements.Length == 1 &&
                !string.Equals(npc.LocationId, placements[0], StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"NPC '{npc.Id}' has conflicting authoritative locations.");
            }
        }

        var puzzles = GetPuzzleDefinitions(definition);
        var puzzleIds = puzzles.Select(puzzle => puzzle.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (puzzleIds.Count != puzzles.Count)
        {
            throw new InvalidOperationException("Adventure puzzle identifiers must be unique.");
        }

        foreach (var puzzle in puzzles)
        {
            foreach (var condition in puzzle.Solutions.SelectMany(solution => solution.Conditions))
            {
                if (condition.Type == PuzzleConditionType.ItemPossessed && !itemIds.Contains(condition.ReferenceId))
                {
                    throw new InvalidOperationException($"Puzzle '{puzzle.Id}' requires unknown item '{condition.ReferenceId}'.");
                }

                if (condition.Type == PuzzleConditionType.PuzzleSolved && !puzzleIds.Contains(condition.ReferenceId))
                {
                    throw new InvalidOperationException($"Puzzle '{puzzle.Id}' references unknown puzzle '{condition.ReferenceId}'.");
                }
            }

            foreach (var prerequisite in puzzle.Prerequisites)
            {
                if (prerequisite.Type == PuzzleConditionType.ItemPossessed && !itemIds.Contains(prerequisite.ReferenceId))
                {
                    throw new InvalidOperationException($"Puzzle '{puzzle.Id}' requires unknown item '{prerequisite.ReferenceId}'.");
                }

                if (prerequisite.Type == PuzzleConditionType.PuzzleSolved && !puzzleIds.Contains(prerequisite.ReferenceId))
                {
                    throw new InvalidOperationException($"Puzzle '{puzzle.Id}' references unknown puzzle '{prerequisite.ReferenceId}'.");
                }
            }

            foreach (var link in puzzle.ChainLinks.Where(link => !puzzleIds.Contains(link)))
            {
                throw new InvalidOperationException($"Puzzle '{puzzle.Id}' links to unknown puzzle '{link}'.");
            }

            foreach (var outcome in puzzle.Outcomes.Where(outcome => outcome.Type == PuzzleOutcomeType.FollowOnPuzzle && !puzzleIds.Contains(outcome.ReferenceId)))
            {
                throw new InvalidOperationException($"Puzzle '{puzzle.Id}' has an outcome referencing unknown puzzle '{outcome.ReferenceId}'.");
            }
        }
    }

    private static IReadOnlyList<PuzzleDefinition> GetPuzzleDefinitions(AdventureDefinition definition) =>
        definition.Puzzles.Count > 0 ? definition.Puzzles : [definition.Puzzle];

    private static Puzzle CreatePuzzle(PuzzleDefinition definition)
    {
        if (definition.Solutions.Count == 0)
        {
            return new Puzzle(new PuzzleId(definition.Id), new PuzzleSolutionCondition(new ItemId(definition.RequiredItemId), definition.RequiredClueKey));
        }

        return new Puzzle(
            new PuzzleId(definition.Id),
            definition.Solutions.Select(solution => new PuzzleSolutionDefinition(
                solution.Id,
                solution.Conditions.Select(condition => new PuzzleCondition(condition.Type, condition.ReferenceId)).ToArray())),
            definition.Prerequisites.Select(prerequisite => new PuzzlePrerequisiteReference(prerequisite.Type, prerequisite.ReferenceId)),
            definition.Outcomes.Select(outcome => new PuzzleOutcomeDefinition(outcome.Id, outcome.Type, outcome.ReferenceId)),
            definition.ChainLinks.Select(link => new PuzzleChainLink(new PuzzleId(link))),
            definition.State);
    }

    private sealed class AdventureDefinition
    {
        public string Id { get; init; } = "";
        public string Title { get; init; } = "";
        public string StartingLocationId { get; init; } = "";
        public List<LocationDefinition> Locations { get; init; } = [];
        public List<ItemDefinition> Items { get; init; } = [];
        public List<NpcDefinition> Npcs { get; init; } = [];
        public List<LoreDefinition> Lore { get; init; } = [];
        public List<TargetVocabularyDefinition> TargetVocabulary { get; init; } = [];
        public PuzzleDefinition Puzzle { get; init; } = new();
        public List<PuzzleDefinition> Puzzles { get; init; } = [];
    }

    private sealed class TargetVocabularyDefinition
    {
        public string Id { get; init; } = "";
        public List<string> Synonyms { get; init; } = [];
    }

    private sealed class LoreDefinition
    {
        public string Id { get; init; } = "";
        public string Classification { get; init; } = "CurrentFact";
        public string? Category { get; init; }
        public string? Scope { get; init; }
        public string ContentKey { get; init; } = "";
        public string Content { get; init; } = "";
        public bool IsProtected { get; init; }
        public List<string> SubjectReferences { get; init; } = [];
    }

    private sealed class LocationDefinition
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public LocationType Type { get; init; } = LocationType.Landmark;
        public EnvironmentalProperties Environment { get; init; } = new(TerrainKind.Forest, "temperate", "daylight");
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
        public List<string> Goals { get; init; } = [];
        public List<string> Relationships { get; init; } = [];
        public string? LocationId { get; init; }
    }

    private sealed class PuzzleDefinition
    {
        public string Id { get; init; } = "";
        public string RequiredItemId { get; init; } = "";
        public string RequiredClueKey { get; init; } = "";
        public PuzzleState State { get; init; } = PuzzleState.Discovered;
        public List<PuzzlePrerequisiteDefinition> Prerequisites { get; init; } = [];
        public List<PuzzleSolutionDefinitionJson> Solutions { get; init; } = [];
        public List<PuzzleOutcomeDefinitionJson> Outcomes { get; init; } = [];
        public List<string> ChainLinks { get; init; } = [];
    }

    private sealed class PuzzlePrerequisiteDefinition
    {
        public PuzzleConditionType Type { get; init; }
        public string ReferenceId { get; init; } = "";
    }

    private sealed class PuzzleConditionDefinition
    {
        public PuzzleConditionType Type { get; init; }
        public string ReferenceId { get; init; } = "";
    }

    private sealed class PuzzleSolutionDefinitionJson
    {
        public string Id { get; init; } = "";
        public List<PuzzleConditionDefinition> Conditions { get; init; } = [];
    }

    private sealed class PuzzleOutcomeDefinitionJson
    {
        public string Id { get; init; } = "";
        public PuzzleOutcomeType Type { get; init; }
        public string ReferenceId { get; init; } = "";
    }
}
