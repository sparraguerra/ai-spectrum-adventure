namespace AI.SpectrumAdventure.Application.Authoring;

using System.Text.Json;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Authoring;
using AI.SpectrumAdventure.Domain.Lore;
using AI.SpectrumAdventure.Domain.Worlds;

public sealed class AdventureAuthoringValidator : IAdventureValidator
{
    private static readonly HashSet<string> Directions = new(StringComparer.OrdinalIgnoreCase) { "North", "East", "South", "West", "Up", "Down" };
    private static readonly HashSet<string> LoreClassifications = new(StringComparer.OrdinalIgnoreCase)
    {
        "ImmutableFact", "HistoricalFact", "RegionalFact", "Rumour", "PlayerDiscoverableKnowledge", "CurrentFact"
    };
    private static readonly HashSet<string> ConditionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ItemPossessed", "ClueKnown", "WorldFlagSet", "PuzzleSolved"
    };
    private static readonly HashSet<string> OutcomeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Clue", "Item", "NpcInteraction", "Location", "Connection", "WorldEvent", "FollowOnPuzzle"
    };
    private static readonly HashSet<string> LoreCategories = new(Enum.GetNames<LoreCategory>(), StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> LoreScopes = new(Enum.GetNames<LoreScope>(), StringComparer.OrdinalIgnoreCase);
    public AuthoringValidationResult Validate(AdventureDraft draft)
    {
        using var telemetry = AuthoringTelemetry.Start("validation");
        var issues = new List<AuthoringValidationIssue>();
        try
        {
            using var document = JsonDocument.Parse(draft.DefinitionJson);
            var root = document.RootElement;
            ValidateDefinitionMetadata(root, draft, issues);
            var locations = Elements(root, "locations");
            var items = Elements(root, "items");
            var npcs = Elements(root, "npcs");
            var lore = Elements(root, "lore");
            var puzzles = Elements(root, "puzzles");
            if (puzzles.Count == 0 && root.TryGetProperty("puzzle", out var legacyPuzzle) && legacyPuzzle.ValueKind == JsonValueKind.Object)
                puzzles = [legacyPuzzle];
            var locationIds = ValidateIdentifiers(locations, "location", issues);
            var itemIds = ValidateIdentifiers(items, "item", issues);
            var npcIds = ValidateIdentifiers(npcs, "NPC", issues);
            var loreIds = ValidateIdentifiers(lore, "lore", issues);
            var puzzleIds = ValidateIdentifiers(puzzles, "puzzle", issues);
            if (locations.Count == 0)
                issues.Add(Block("locations", "The adventure must define at least one location."));
            ValidateLore(lore, locationIds, itemIds, npcIds, puzzleIds, issues);
            ValidateNpcs(npcs, locations, npcIds, locationIds, lore, issues);
            ValidatePuzzles(puzzles, puzzleIds, locationIds, itemIds, npcIds, lore, issues);
            var start = draft.StartingLocationId;
            if (!locationIds.Contains(start))
            {
                issues.Add(Block("startingLocation", $"Starting location '{start}' does not exist."));
            }

            var adjacency = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var location in locations)
            {
                var locationId = StringValue(location, "id");
                if (locationId is null) continue;
                ValidateLocationMetadata(location, locationId, issues);
                adjacency[locationId] = [];
                var directions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var exit in Elements(location, "exits"))
                {
                    var direction = StringValue(exit, "direction");
                    var destination = StringValue(exit, "to");
                    if (string.IsNullOrWhiteSpace(direction) || !Directions.Contains(direction))
                        issues.Add(Block($"location:{locationId}:exit", $"Location '{locationId}' has an invalid direction '{direction ?? ""}'."));
                    else if (!directions.Add(direction))
                        issues.Add(Block($"location:{locationId}:exit:{direction}", $"Location '{locationId}' has duplicate exits in direction '{direction}'."));
                    if (string.IsNullOrWhiteSpace(destination) || !locationIds.Contains(destination))
                        issues.Add(Block($"location:{locationId}:exit", $"Location '{locationId}' has an exit to unknown location '{destination ?? ""}'."));
                    else adjacency[locationId].Add(destination);
                }

                foreach (var objectId in StringValues(location, "objectIds").Where(id => !itemIds.Contains(id)))
                    issues.Add(Block($"location:{locationId}:item:{objectId}", $"Location '{locationId}' references unknown item '{objectId}'."));

                foreach (var npcId in StringValues(location, "npcIds").Where(id => !npcIds.Contains(id)))
                    issues.Add(Block($"location:{locationId}:npc:{npcId}", $"Location '{locationId}' references unknown NPC '{npcId}'."));
            }

            var reachable = Reachable(start, adjacency);
            var hasReachableMeaningfulContent = locations.Any(location =>
            {
                var locationId = StringValue(location, "id");
                return locationId is not null && reachable.Contains(locationId)
                    && (Elements(location, "objectIds").Count > 0 || StringValues(location, "npcIds").Any());
            }) || (locationIds.Contains(start) && (npcs.Count > 0 || lore.Count > 0 || puzzles.Count > 0));
            if (locationIds.Contains(start) && reachable.Count == 1 && !hasReachableMeaningfulContent)
                issues.Add(Block("world", "The starting location cannot reach another location or meaningful interaction."));
        }
        catch (JsonException exception)
        {
            issues.Add(Block("definition", $"Adventure definition is not valid JSON: {exception.Message}"));
        }
        catch (InvalidOperationException exception)
        {
            issues.Add(Block("definition", $"Adventure definition has an invalid shape: {exception.Message}"));
        }

        return new AuthoringValidationResult(draft.Id, draft.Revision, issues);
    }

    private static void ValidateLore(
        IReadOnlyList<JsonElement> lore,
        ISet<string> locationIds,
        ISet<string> itemIds,
        ISet<string> npcIds,
        ISet<string> puzzleIds,
        ICollection<AuthoringValidationIssue> issues)
    {
        var protectedFacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in lore)
        {
            var id = StringValue(entry, "id") ?? "lore";
            var classification = StringValue(entry, "classification") ?? StringValue(entry, "truthClassification");
            if (string.IsNullOrWhiteSpace(classification) || !LoreClassifications.Contains(classification))
                issues.Add(Block($"lore:{id}", $"Lore '{id}' has an invalid classification '{classification ?? ""}'."));

            var category = StringValue(entry, "category");
            if (!string.IsNullOrWhiteSpace(category) && !LoreCategories.Contains(category))
                issues.Add(Block($"lore:{id}", $"Lore '{id}' has an invalid category '{category}'."));
            var scope = StringValue(entry, "scope");
            if (!string.IsNullOrWhiteSpace(scope) && !LoreScopes.Contains(scope))
                issues.Add(Block($"lore:{id}", $"Lore '{id}' has an invalid scope '{scope}'."));

            var subjectReferences = StringValues(entry, "subjectReferences").ToArray();
            if (string.Equals(classification, "PlayerDiscoverableKnowledge", StringComparison.OrdinalIgnoreCase)
                && subjectReferences.Length == 0)
            {
                issues.Add(Block($"lore:{id}", $"Player-discoverable lore '{id}' must define at least one subject reference."));
            }
            foreach (var reference in subjectReferences)
            {
                var known = scope switch
                {
                    "Location" => locationIds.Contains(reference),
                    "Character" => npcIds.Contains(reference),
                    "Object" => itemIds.Contains(reference),
                    "Puzzle" => puzzleIds.Contains(reference),
                    _ => true
                };
                if (!known)
                    issues.Add(Block($"lore:{id}:subject:{reference}", $"Lore '{id}' references an unknown {scope} subject '{reference}'."));
            }

            var isProtected = BooleanValue(entry, "isProtected") || BooleanValue(entry, "protected") ||
                string.Equals(classification, "ImmutableFact", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(classification, "HistoricalFact", StringComparison.OrdinalIgnoreCase);
            if (!isProtected) continue;
            var key = StringValue(entry, "contentKey") ?? id;
            var content = StringValue(entry, "content") ?? StringValue(entry, "value") ?? string.Empty;
            if (protectedFacts.TryGetValue(key, out var existing) && !string.Equals(existing, content, StringComparison.Ordinal))
                issues.Add(Block($"lore:{id}", $"Protected lore '{key}' contradicts another protected fact."));
            else protectedFacts[key] = content;
        }
    }

    private static void ValidateNpcs(IReadOnlyList<JsonElement> npcs, IReadOnlyList<JsonElement> locations,
        ISet<string> npcIds, ISet<string> locationIds, IReadOnlyList<JsonElement> lore, ICollection<AuthoringValidationIssue> issues)
    {
        var loreKeys = lore.SelectMany(entry => new[] { StringValue(entry, "id"), StringValue(entry, "contentKey") })
            .Where(key => !string.IsNullOrWhiteSpace(key)).Select(key => key!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var npc in npcs)
        {
            var npcId = StringValue(npc, "id") ?? "npc";
            var locationId = StringValue(npc, "locationId");
            if (locationId is not null && !locationIds.Contains(locationId))
                issues.Add(Block($"npc:{npcId}:location", $"NPC '{npcId}' references unknown location '{locationId}'."));
            var placementIds = locations
                .Where(location => StringValues(location, "npcIds").Contains(npcId, StringComparer.OrdinalIgnoreCase))
                .Select(location => StringValue(location, "id"))
                .Where(id => id is not null)
                .ToArray();
            if (locationId is null && placementIds.Length == 0)
                issues.Add(Block($"npc:{npcId}:location", $"NPC '{npcId}' has no authoritative location."));
            if (placementIds.Length > 1)
                issues.Add(Block($"npc:{npcId}:location", $"NPC '{npcId}' has multiple authoritative locations."));
            if (locationId is not null && placementIds.Length == 1 && !string.Equals(locationId, placementIds[0], StringComparison.OrdinalIgnoreCase))
                issues.Add(Block($"npc:{npcId}:location", $"NPC '{npcId}' authoritative location conflicts with its location placement."));
            foreach (var knowledge in StringValues(npc, "knowledgeBoundary"))
            {
                if (lore.Count > 0 && !loreKeys.Contains(knowledge))
                    issues.Add(Block($"npc:{npcId}:knowledge:{knowledge}", $"NPC '{npcId}' references unknown lore or knowledge '{knowledge}'."));
            }
        }
    }

    private static void ValidatePuzzles(IReadOnlyList<JsonElement> puzzles, ISet<string> puzzleIds, ISet<string> locationIds,
        ISet<string> itemIds, ISet<string> npcIds, IReadOnlyList<JsonElement> lore, ICollection<AuthoringValidationIssue> issues)
    {
        var loreKeys = lore.SelectMany(entry => new[] { StringValue(entry, "id"), StringValue(entry, "contentKey") })
            .Where(key => !string.IsNullOrWhiteSpace(key)).Select(key => key!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var puzzle in puzzles)
        {
            var puzzleId = StringValue(puzzle, "id") ?? "puzzle";
            var solutions = Elements(puzzle, "solutions");
            var solutionIds = ValidateIdentifiers(solutions, $"solution for puzzle '{puzzleId}'", issues);
            if (solutions.Count == 0 && (StringValue(puzzle, "requiredItemId") is not null || StringValue(puzzle, "requiredClueKey") is not null))
                continue;
            foreach (var prerequisite in Elements(puzzle, "prerequisites"))
                ValidatePuzzleReference(puzzleId, "prerequisite", prerequisite, puzzleIds, locationIds, itemIds, npcIds, loreKeys, issues);
            foreach (var solution in solutions)
            {
                var solutionId = StringValue(solution, "id") ?? "solution";
                if (Elements(solution, "conditions").Count == 0)
                    issues.Add(Block($"puzzle:{puzzleId}:solution:{solutionId}", $"Puzzle solution '{solutionId}' must define at least one condition."));
                foreach (var condition in Elements(solution, "conditions"))
                    ValidatePuzzleReference(puzzleId, $"solution:{solutionId}", condition, puzzleIds, locationIds, itemIds, npcIds, loreKeys, issues);
            }
            foreach (var outcome in Elements(puzzle, "outcomes"))
                ValidatePuzzleReference(puzzleId, "outcome", outcome, puzzleIds, locationIds, itemIds, npcIds, loreKeys, issues);
            foreach (var link in StringValues(puzzle, "chainLinks").Where(link => !puzzleIds.Contains(link)))
                issues.Add(Block($"puzzle:{puzzleId}:chain:{link}", $"Puzzle '{puzzleId}' links to unknown puzzle '{link}'."));
        }
    }

    private static void ValidatePuzzleReference(string puzzleId, string context, JsonElement reference,
        ISet<string> puzzleIds, ISet<string> locationIds, ISet<string> itemIds, ISet<string> npcIds,
        ISet<string> loreKeys, ICollection<AuthoringValidationIssue> issues)
    {
        var type = StringValue(reference, "type");
        var id = StringValue(reference, "referenceId") ?? StringValue(reference, "id");
        if (string.Equals(context, "outcome", StringComparison.Ordinal) && !OutcomeTypes.Contains(type ?? string.Empty))
            issues.Add(Block($"puzzle:{puzzleId}:outcome", $"Puzzle '{puzzleId}' has an invalid outcome type '{type ?? ""}'."));
        else if (!string.Equals(context, "outcome", StringComparison.Ordinal) && !ConditionTypes.Contains(type ?? string.Empty))
            issues.Add(Block($"puzzle:{puzzleId}:{context}", $"Puzzle '{puzzleId}' has an invalid condition type '{type ?? ""}'."));
        if (string.Equals(type, "WorldFlagSet", StringComparison.OrdinalIgnoreCase)) return;
        var known = type switch
        {
            "ItemPossessed" or "Item" => itemIds.Contains(id ?? string.Empty),
            "PuzzleSolved" or "FollowOnPuzzle" => puzzleIds.Contains(id ?? string.Empty),
            "ClueKnown" or "Clue" => loreKeys.Contains(id ?? string.Empty),
            "Location" or "Connection" => locationIds.Contains(id ?? string.Empty),
            "NpcInteraction" => npcIds.Contains(id ?? string.Empty),
            "WorldFlagSet" or "WorldEvent" => true,
            _ => false
        };
        if (!known) issues.Add(Block($"puzzle:{puzzleId}:{context}", $"Puzzle '{puzzleId}' references unknown {type ?? "element"} '{id ?? ""}'."));
    }

    private static void ValidateLocationMetadata(JsonElement location, string locationId, ICollection<AuthoringValidationIssue> issues)
    {
        if (location.TryGetProperty("type", out var typeElement) && typeElement.ValueKind != JsonValueKind.Null)
        {
            var type = StringValue(location, "type");
            if (type is null || !Enum.TryParse<LocationType>(type, true, out _))
                issues.Add(Block($"location:{locationId}:type", $"Location '{locationId}' has an invalid location type '{type ?? ""}'."));
        }

        if (!location.TryGetProperty("environment", out var environment)) return;
        if (environment.ValueKind != JsonValueKind.Object)
        {
            issues.Add(Block($"location:{locationId}:environment", $"Location '{locationId}' has invalid environmental properties."));
            return;
        }

        var terrain = StringValue(environment, "terrain");
        if (terrain is null || !Enum.TryParse<TerrainKind>(terrain, true, out _))
            issues.Add(Block($"location:{locationId}:environment", $"Location '{locationId}' has an invalid terrain '{terrain ?? ""}'."));
        if (string.IsNullOrWhiteSpace(StringValue(environment, "climate")))
            issues.Add(Block($"location:{locationId}:environment", $"Location '{locationId}' must define an environmental climate."));
        if (string.IsNullOrWhiteSpace(StringValue(environment, "lighting")))
            issues.Add(Block($"location:{locationId}:environment", $"Location '{locationId}' must define environmental lighting."));
    }

    private static void ValidateDefinitionMetadata(JsonElement root, AdventureDraft draft, ICollection<AuthoringValidationIssue> issues)
    {
        var definitionId = StringValue(root, "id");
        if (definitionId is not null && !string.Equals(definitionId, draft.AdventureIdentifier, StringComparison.OrdinalIgnoreCase))
            issues.Add(Block("adventure:id", "The definition identifier does not match the draft identifier."));

        var definitionTitle = StringValue(root, "title");
        if (definitionTitle is not null && !string.Equals(definitionTitle, draft.Title, StringComparison.Ordinal))
            issues.Add(Block("adventure:title", "The definition title does not match the draft title."));

        var definitionStart = StringValue(root, "startingLocationId");
        if (definitionStart is not null && !string.Equals(definitionStart, draft.StartingLocationId, StringComparison.OrdinalIgnoreCase))
            issues.Add(Block("adventure:startingLocation", "The definition starting location does not match the draft starting location."));
    }

    private static HashSet<string> ValidateIdentifiers(IReadOnlyList<JsonElement> elements, string kind, ICollection<AuthoringValidationIssue> issues)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in elements)
        {
            var id = StringValue(element, "id");
            if (string.IsNullOrWhiteSpace(id) || id.Any(character => char.IsWhiteSpace(character)))
            {
                issues.Add(Block(kind, $"Every {kind} must have a non-empty identifier without whitespace."));
                continue;
            }

            if (!ids.Add(id)) issues.Add(Block($"{kind}:{id}", $"Duplicate {kind} identifier '{id}'."));
        }

        return ids;
    }

    private static List<JsonElement> Elements(JsonElement parent, string property) =>
        parent.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().ToList()
            : [];

    private static string? StringValue(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool BooleanValue(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;
    private static IEnumerable<string> StringValues(JsonElement element, string property) =>
        Elements(element, property).Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString()!).Where(value => !string.IsNullOrWhiteSpace(value));

    private static HashSet<string> Reachable(string start, IReadOnlyDictionary<string, List<string>> adjacency)
    {
        var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<string>();
        pending.Enqueue(start);
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            if (!reachable.Add(current)) continue;
            foreach (var destination in adjacency.GetValueOrDefault(current) ?? []) pending.Enqueue(destination);
        }
        return reachable;
    }

    private static AuthoringValidationIssue Block(string element, string reason) => new(element, reason, AuthoringIssueSeverity.Blocking);
}