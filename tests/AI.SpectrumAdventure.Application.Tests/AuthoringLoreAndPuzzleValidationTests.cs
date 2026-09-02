namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Domain.Authoring;
using FluentAssertions;

public sealed class AuthoringLoreAndPuzzleValidationTests
{
    private readonly AdventureAuthoringValidator validator = new();

    [Fact]
    public void Validate_ReportsInvalidLoreAndContradictoryProtectedFacts()
    {
        var draft = Draft("start", """
            { "locations": [{ "id": "start", "objectIds": ["key"], "npcIds": ["guide"] }],
              "items": [{ "id": "key" }],
              "npcs": [{ "id": "guide", "knowledgeBoundary": ["missing-lore"] }],
              "lore": [
                { "id": "fact-a", "classification": "ImmutableFact", "contentKey": "door", "content": "Open" },
                { "id": "fact-b", "classification": "ImmutableFact", "contentKey": "door", "content": "Closed" },
                { "id": "rumour", "classification": "NotAClassification", "contentKey": "rumour", "content": "Maybe" }
              ]
            }
            """);

        var result = validator.Validate(draft);

        result.Issues.Should().Contain(issue => issue.Reason.Contains("unknown lore or knowledge"));
        result.Issues.Should().Contain(issue => issue.Reason.Contains("contradicts"));
        result.Issues.Should().Contain(issue => issue.Reason.Contains("invalid classification"));
    }

    [Fact]
    public void Validate_ReportsMissingPuzzleReferencesAndAcceptsAlternativesAndChains()
    {
        var json = DraftDefinitionEditor.UpsertPuzzle("""
            { "locations": [{ "id": "start", "objectIds": ["key"], "npcIds": ["guide"] }],
              "items": [{ "id": "key" }],
              "npcs": [{ "id": "guide", "knowledgeBoundary": ["tower-clue"] }],
              "lore": [{ "id": "clue", "contentKey": "tower-clue", "classification": "PlayerDiscoverableKnowledge", "content": "A clue." }]
            }
            """, new AuthoringPuzzle("first", [new("ItemPossessed", "key")],
                [new("item-solution", [new("ItemPossessed", "key")]), new("clue-solution", [new("ClueKnown", "tower-clue")])],
                [new("next", "FollowOnPuzzle", "second")], ["second"]));
        json = DraftDefinitionEditor.UpsertPuzzle(json, new AuthoringPuzzle("second", Solutions: [new("finish", [new("ItemPossessed", "key")])]));

        var valid = validator.Validate(Draft("start", json));
        valid.HasBlockingIssues.Should().BeFalse();

        var invalid = validator.Validate(Draft("start", json.Replace("\"referenceId\":\"key\"", "\"referenceId\":\"absent-item\"", StringComparison.Ordinal)));
        invalid.Issues.Should().Contain(issue => issue.Reason.Contains("unknown ItemPossessed 'absent-item'"));
    }

    [Fact]
    public void DefinitionEditor_UpsertsNpcLorePuzzleAndChainData()
    {
        var json = DraftDefinitionEditor.UpsertLocation("{}", new AuthoringLocation("start", "Start", "A path."));
        json = DraftDefinitionEditor.UpsertNpc(json, new AuthoringNpc("guide", "Guide", "Patient", LocationId: "start"));
        json = DraftDefinitionEditor.UpsertLore(json, new AuthoringLore("clue", "PlayerDiscoverableKnowledge", "tower-clue", "A clue."));
        json = DraftDefinitionEditor.UpsertPuzzle(json, new AuthoringPuzzle("first"));
        json = DraftDefinitionEditor.UpsertPuzzle(json, new AuthoringPuzzle("second"));
        json = DraftDefinitionEditor.UpsertPuzzleSolution(json, "first", new AuthoringPuzzleSolution("alternate", [new("ClueKnown", "tower-clue")]));
        json = DraftDefinitionEditor.UpsertPuzzleOutcome(json, "first", new AuthoringPuzzleOutcome("continue", "FollowOnPuzzle", "second"));
        json = DraftDefinitionEditor.AddPuzzleChainLink(json, "first", "second");

        json.Should().Contain("guide").And.Contain("tower-clue").And.Contain("alternate").And.Contain("second");
    }

    private static AdventureDraft Draft(string start, string definition) =>
        new(AdventureDraftId.New(), "advanced-test", "Advanced Test", start, definition);
}
