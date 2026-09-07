namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class AdvancedAuthoringFlowTests
{
    [Fact]
    public async Task SaveAndReload_PersistsNpcLoreAndLinkedMultiSolutionPuzzles()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new EfAdventureAuthoringRepository(context);
        var useCases = new DraftUseCases(repository, new AdventureAuthoringValidator());
        var definition = """
            {
              "id": "advanced-trail",
              "title": "Advanced Trail",
              "locations": [{ "id": "start", "name": "Start", "description": "A path.", "npcIds": ["guide"], "objectIds": ["key"] }],
              "items": [{ "id": "key", "name": "Key", "description": "A key." }],
              "npcs": [{ "id": "guide", "name": "Guide", "personality": "Patient", "knowledgeBoundary": ["tower-clue"] }],
              "lore": [{ "id": "clue", "classification": "PlayerDiscoverableKnowledge", "contentKey": "tower-clue", "content": "The old door opens." }],
              "puzzles": [
                { "id": "first", "solutions": [
                  { "id": "key-solution", "conditions": [{ "type": "ItemPossessed", "referenceId": "key" }] },
                  { "id": "clue-solution", "conditions": [{ "type": "ClueKnown", "referenceId": "tower-clue" }] }
                ], "outcomes": [{ "id": "continue", "type": "FollowOnPuzzle", "referenceId": "second" }], "chainLinks": ["second"] },
                { "id": "second", "solutions": [{ "id": "finish", "conditions": [{ "type": "ItemPossessed", "referenceId": "key" }] }] }
              ]
            }
            """;

        var created = await useCases.CreateAsync(new AdventureDraftRequest("advanced-trail", "Advanced Trail", "start", definition));
        created.Succeeded.Should().BeTrue();

        var reloaded = await repository.FindDraftByIdentifierAsync("advanced-trail");
        reloaded.Should().NotBeNull();
        new AdventureAuthoringValidator().Validate(reloaded!).HasBlockingIssues.Should().BeFalse();
        reloaded!.DefinitionJson.Should().Contain("guide").And.Contain("tower-clue").And.Contain("key-solution").And.Contain("second");
    }
}
