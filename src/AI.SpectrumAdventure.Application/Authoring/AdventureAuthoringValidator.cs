namespace AI.SpectrumAdventure.Application.Authoring;

using System.Text.Json;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Authoring;

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
    public AuthoringValidationResult Validate(AdventureDraft draft)
    {
        using var telemetry = AuthoringTelemetry.Start("validation");
        var issues = new List<AuthoringValidationIssue>();
        try
        {
            using var document = JsonDocument.Parse(draft.DefinitionJson);
            var root = document.RootElement;
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
            ValidateLore(lore, issues);
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

    private static void ValidateLore(IReadOnlyList<JsonElement> lore, ICollection<AuthoringValidationIssue> issues)
    {
        var protectedFacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in lore)
        {
            var id = StringValue(entry, "id") ?? "lore";
            var classification = StringValue(entry, "classification") ?? StringValue(entry, "truthClassification");
            if (string.IsNullOrWhiteSpace(classification) || !LoreClassifications.Contains(classification))
                issues.Add(Block($"lore:{id}", $"Lore '{id}' has an invalid classification '{classification ?? ""}'."));

            var isProtected = BooleanValue(entry, "isProtected") || BooleanValue(entry, "protected") ||
                string.Equals(classification, "ImmutableFact", StringComparison.OrdinalIgnoreCase);
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
            var placements = locations.Count(location => StringValues(location, "npcIds").Contains(npcId, StringComparer.OrdinalIgnoreCase));
            if (locationId is null && placements == 0)
                issues.Add(Block($"npc:{npcId}:location", $"NPC '{npcId}' has no authoritative location."));
            if (placements > 1)
                issues.Add(Block($"npc:{npcId}:location", $"NPC '{npcId}' has multiple authoritative locations."));
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