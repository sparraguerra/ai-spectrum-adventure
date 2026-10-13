namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Domain.Authoring;
using AI.SpectrumAdventure.Domain.Worlds;
using FluentAssertions;

public sealed class AdventureAuthoringValidatorTests
{
    private readonly AdventureAuthoringValidator validator = new();

    [Fact]
    public void Validate_ReportsDuplicateIdsAndInvalidDestination()
    {
        var draft = Draft("start", """
            { "locations": [
              { "id": "start", "exits": [{ "direction": "North", "to": "missing" }] },
              { "id": "start" }
            ] }
            """);

        var result = validator.Validate(draft);

        result.Issues.Should().Contain(issue => issue.Reason.Contains("Duplicate location identifier"));
        result.Issues.Should().Contain(issue => issue.Reason.Contains("unknown location 'missing'"));
    }

    [Fact]
    public void Validate_ReportsInvalidAndDuplicateDirections()
    {
        var draft = Draft("start", """
            { "locations": [
              { "id": "start", "exits": [
                { "direction": "Forward", "to": "next" },
                { "direction": "North", "to": "next" },
                { "direction": "north", "to": "next" }
              ] },
              { "id": "next", "objectIds": ["key"] }
            ], "items": [{ "id": "key" }],
              "lore": [{ "id": "lore", "classification": "PlayerDiscoverableKnowledge", "category": "NoCategory", "scope": "NoScope", "contentKey": "key", "content": "A fact." }] }
            """);

        var result = validator.Validate(draft);

        result.Issues.Should().Contain(issue => issue.Reason.Contains("invalid direction"));
        result.Issues.Should().Contain(issue => issue.Reason.Contains("duplicate exits"));
    }

    [Fact]
    public void Validate_ReportsUnreachableStartWorld()
    {
        var draft = Draft("isolated", """
            { "locations": [
              { "id": "isolated" },
              { "id": "other", "objectIds": ["key"] }
            ], "items": [{ "id": "key" }] }
            """);

        var result = validator.Validate(draft);

        result.Issues.Should().Contain(issue => issue.Element == "world");
    }

      [Fact]
      public void Validate_ReportsTopLevelDefinitionMetadataDrift()
      {
        var draft = Draft("start", """
          { "id": "different-id", "title": "Different title", "startingLocationId": "other-start",
            "locations": [{ "id": "start", "objectIds": ["key"] }, { "id": "other-start" }],
            "items": [{ "id": "key" }] }
          """);

        var result = validator.Validate(draft);

        result.Issues.Should().Contain(issue => issue.Reason.Contains("identifier does not match"));
        result.Issues.Should().Contain(issue => issue.Reason.Contains("title does not match"));
        result.Issues.Should().Contain(issue => issue.Reason.Contains("starting location does not match"));
      }

      [Fact]
      public void Validate_RejectsUnknownLocationTypeAndEnvironmentValues()
      {
        var draft = Draft("start", """
          { "locations": [{
            "id": "start", "type": "SkyCastle",
            "environment": { "terrain": "Void", "climate": "cold", "lighting": "night" },
            "objectIds": ["key"]
          }], "items": [{ "id": "key" }],
          "lore": [{ "id": "lore", "classification": "PlayerDiscoverableKnowledge", "category": "NoCategory", "scope": "NoScope", "contentKey": "key", "content": "A fact." }] }
          """);

        var result = validator.Validate(draft);

        result.Issues.Should().Contain(issue => issue.Reason.Contains("invalid location type"));
        result.Issues.Should().Contain(issue => issue.Reason.Contains("invalid terrain"));
        result.Issues.Should().Contain(issue => issue.Reason.Contains("invalid category"));
        result.Issues.Should().Contain(issue => issue.Reason.Contains("invalid scope"));
      }

      [Fact]
      public void Validate_RejectsNpcLocationMetadataThatConflictsWithLocationPlacement()
      {
        var draft = Draft("start", """
          { "locations": [
            { "id": "start", "npcIds": ["guide"] },
            { "id": "elsewhere" }
          ], "npcs": [{ "id": "guide", "locationId": "elsewhere" }] }
          """);

        var result = validator.Validate(draft);

        result.Issues.Should().Contain(issue => issue.Reason.Contains("authoritative location conflicts"));
      }

      [Fact]
      public void DefinitionEditor_UpsertsWorldElementsAndPreservesRuntimeShape()
      {
        var json = DraftDefinitionEditor.SetStartingLocation("{}", "start");
        json = DraftDefinitionEditor.UpsertLocation(json, new AuthoringLocation(
          "start", "Start", "A path.",
          Type: LocationType.Path,
          Environment: new EnvironmentalProperties(TerrainKind.Marsh, "humid", "dusk", ["reeds"])));
        json = DraftDefinitionEditor.UpsertLocation(json, new AuthoringLocation("end", "End", "A clearing."));
        json = DraftDefinitionEditor.UpsertConnection(json, "start", new AuthoringExit("East", "end"));
        json = DraftDefinitionEditor.UpsertItem(json, new AuthoringItem("token", "Token", "A token."));

        var draft = Draft("start", json);

        validator.Validate(draft).HasBlockingIssues.Should().BeFalse();
        json.Should().Contain("\"objectIds\":[]").And.Contain("\"states\":[]")
          .And.Contain("\"type\":\"Path\"")
          .And.Contain("\"terrain\":\"Marsh\"")
          .And.Contain("\"features\":[\"reeds\"]");
      }

    private static AdventureDraft Draft(string start, string definition) =>
        new(AdventureDraftId.New(), "validator-test", "Validator Test", start, definition);
}