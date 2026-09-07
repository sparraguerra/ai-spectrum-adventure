namespace AI.SpectrumAdventure.Application.Authoring;

using System.Text.Json;
using System.Text.Json.Nodes;

public sealed record AuthoringLocation(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<AuthoringExit>? Exits = null,
    IReadOnlyList<string>? ObjectIds = null,
    IReadOnlyList<string>? NpcIds = null);

public sealed record AuthoringExit(string Direction, string To, string? RequiredFlagKey = null, bool MustBeSet = true);

public sealed record AuthoringItem(string Id, string Name, string Description, IReadOnlyList<string>? States = null);

public sealed record AuthoringNpc(
    string Id,
    string Name,
    string Personality,
    IReadOnlyList<string>? KnowledgeBoundary = null,
    IReadOnlyList<string>? Goals = null,
    IReadOnlyList<string>? Relationships = null,
    string? LocationId = null);

public sealed record AuthoringLore(
    string Id,
    string Classification,
    string ContentKey,
    string Content,
    bool IsProtected = false,
    IReadOnlyList<string>? SubjectReferences = null);

public sealed record AuthoringPuzzleCondition(string Type, string ReferenceId);
public sealed record AuthoringPuzzlePrerequisite(string Type, string ReferenceId);
public sealed record AuthoringPuzzleSolution(string Id, IReadOnlyList<AuthoringPuzzleCondition>? Conditions = null);
public sealed record AuthoringPuzzleOutcome(string Id, string Type, string ReferenceId);
public sealed record AuthoringPuzzle(
    string Id,
    IReadOnlyList<AuthoringPuzzlePrerequisite>? Prerequisites = null,
    IReadOnlyList<AuthoringPuzzleSolution>? Solutions = null,
    IReadOnlyList<AuthoringPuzzleOutcome>? Outcomes = null,
    IReadOnlyList<string>? ChainLinks = null,
    string? State = null);

public static class DraftDefinitionEditor
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string UpsertLocation(string definitionJson, AuthoringLocation location)
    {
        var root = Parse(definitionJson);
        var locations = GetArray(root, "locations");
        var value = JsonSerializer.SerializeToNode(location with
        {
            Exits = location.Exits ?? [],
            ObjectIds = location.ObjectIds ?? [],
            NpcIds = location.NpcIds ?? []
        }, Options)!.AsObject();
        ReplaceById(locations, value, location.Id);
        return root.ToJsonString(Options);
    }

    public static string UpsertConnection(string definitionJson, string sourceLocationId, AuthoringExit connection)
    {
        var root = Parse(definitionJson);
        var locations = GetArray(root, "locations");
        var source = locations.OfType<JsonObject>().FirstOrDefault(location => SameId(location, sourceLocationId));
        if (source is null)
        {
            throw new InvalidOperationException($"Source location '{sourceLocationId}' does not exist.");
        }

        var exits = GetArray(source, "exits");
        var value = JsonSerializer.SerializeToNode(connection, Options)!.AsObject();
        var existing = exits.OfType<JsonObject>().FirstOrDefault(exit => string.Equals(exit["direction"]?.GetValue<string>(), connection.Direction, StringComparison.OrdinalIgnoreCase));
        if (existing is null) exits.Add(value); else exits[exits.IndexOf(existing)] = value;
        return root.ToJsonString(Options);
    }

    public static string UpsertItem(string definitionJson, AuthoringItem item)
    {
        var root = Parse(definitionJson);
        var items = GetArray(root, "items");
        var value = JsonSerializer.SerializeToNode(item with { States = item.States ?? [] }, Options)!.AsObject();
        ReplaceById(items, value, item.Id);
        return root.ToJsonString(Options);
    }

    public static string SetStartingLocation(string definitionJson, string locationId)
    {
        var root = Parse(definitionJson);
        root["startingLocationId"] = locationId;
        return root.ToJsonString(Options);
    }

    public static string UpsertNpc(string definitionJson, AuthoringNpc npc)
    {
        var root = Parse(definitionJson);
        var npcs = GetArray(root, "npcs");
        var value = JsonSerializer.SerializeToNode(npc with
        {
            KnowledgeBoundary = npc.KnowledgeBoundary ?? [],
            Goals = npc.Goals ?? [],
            Relationships = npc.Relationships ?? []
        }, Options)!.AsObject();
        ReplaceById(npcs, value, npc.Id);
        if (!string.IsNullOrWhiteSpace(npc.LocationId))
        {
            var location = FindById(GetArray(root, "locations"), npc.LocationId);
            var npcIds = GetArray(location, "npcIds");
            if (!npcIds.Any(existing => existing is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var existingId) && string.Equals(existingId, npc.Id, StringComparison.OrdinalIgnoreCase)))
                npcIds.Add(npc.Id);
        }
        return root.ToJsonString(Options);
    }

    public static string UpsertLore(string definitionJson, AuthoringLore lore)
    {
        var root = Parse(definitionJson);
        var loreEntries = GetArray(root, "lore");
        var value = JsonSerializer.SerializeToNode(lore with { SubjectReferences = lore.SubjectReferences ?? [] }, Options)!.AsObject();
        ReplaceById(loreEntries, value, lore.Id);
        return root.ToJsonString(Options);
    }

    public static string UpsertPuzzle(string definitionJson, AuthoringPuzzle puzzle)
    {
        var root = Parse(definitionJson);
        var puzzles = GetArray(root, "puzzles");
        var value = JsonSerializer.SerializeToNode(puzzle with
        {
            Prerequisites = puzzle.Prerequisites ?? [],
            Solutions = puzzle.Solutions ?? [],
            Outcomes = puzzle.Outcomes ?? [],
            ChainLinks = puzzle.ChainLinks ?? []
        }, Options)!.AsObject();
        ReplaceById(puzzles, value, puzzle.Id);
        return root.ToJsonString(Options);
    }

    public static string UpsertPuzzleSolution(string definitionJson, string puzzleId, AuthoringPuzzleSolution solution)
    {
        var root = Parse(definitionJson);
        var puzzle = FindById(GetArray(root, "puzzles"), puzzleId);
        var solutions = GetArray(puzzle, "solutions");
        var value = JsonSerializer.SerializeToNode(solution with { Conditions = solution.Conditions ?? [] }, Options)!.AsObject();
        ReplaceById(solutions, value, solution.Id);
        return root.ToJsonString(Options);
    }

    public static string UpsertPuzzleOutcome(string definitionJson, string puzzleId, AuthoringPuzzleOutcome outcome)
    {
        var root = Parse(definitionJson);
        var puzzle = FindById(GetArray(root, "puzzles"), puzzleId);
        var outcomes = GetArray(puzzle, "outcomes");
        var value = JsonSerializer.SerializeToNode(outcome, Options)!.AsObject();
        ReplaceById(outcomes, value, outcome.Id);
        return root.ToJsonString(Options);
    }

    public static string AddPuzzleChainLink(string definitionJson, string puzzleId, string nextPuzzleId)
    {
        var root = Parse(definitionJson);
        var puzzle = FindById(GetArray(root, "puzzles"), puzzleId);
        var links = GetArray(puzzle, "chainLinks");
        if (!links.Any(link => link is JsonValue value && value.TryGetValue<string>(out var linkId) && string.Equals(linkId, nextPuzzleId, StringComparison.OrdinalIgnoreCase))) links.Add(nextPuzzleId);
        return root.ToJsonString(Options);
    }

    private static JsonObject Parse(string json) => JsonNode.Parse(json)?.AsObject()
        ?? throw new InvalidOperationException("Adventure definition must be a JSON object.");

    private static JsonArray GetArray(JsonObject parent, string property)
    {
        if (parent[property] is not JsonArray array)
        {
            array = [];
            parent[property] = array;
        }

        return array;
    }

    private static void ReplaceById(JsonArray values, JsonObject replacement, string id)
    {
        var existing = values.OfType<JsonObject>().FirstOrDefault(value => SameId(value, id));
        if (existing is null) values.Add(replacement); else values[values.IndexOf(existing)] = replacement;
    }

    private static JsonObject FindById(JsonArray values, string id) =>
        values.OfType<JsonObject>().FirstOrDefault(value => SameId(value, id))
        ?? throw new InvalidOperationException($"Puzzle '{id}' does not exist.");

    private static bool SameId(JsonObject value, string id) =>
        string.Equals(value["id"]?.GetValue<string>(), id, StringComparison.OrdinalIgnoreCase);
}