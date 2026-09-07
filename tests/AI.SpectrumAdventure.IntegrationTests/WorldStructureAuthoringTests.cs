namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class WorldStructureAuthoringTests
{
    [Fact]
    public async Task SaveAndReload_TraversableThreeLocationDefinition()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new EfAdventureAuthoringRepository(context);
        var useCases = new DraftUseCases(repository, new AdventureAuthoringValidator());
        var definition = """
            {
              "id": "trail",
              "title": "Trail",
              "locations": [
                { "id": "start", "name": "Start", "description": "A path.", "exits": [{ "direction": "East", "to": "middle" }] },
                { "id": "middle", "name": "Middle", "description": "A fork.", "exits": [{ "direction": "East", "to": "end" }, { "direction": "West", "to": "start" }] },
                { "id": "end", "name": "End", "description": "A clearing.", "exits": [{ "direction": "West", "to": "middle" }], "objectIds": ["token"] }
              ],
              "items": [{ "id": "token", "name": "Token", "description": "A token." }]
            }
            """;

        var created = await useCases.CreateAsync(new AdventureDraftRequest("trail", "Trail", "start", definition));
        created.Succeeded.Should().BeTrue();

        var reloaded = await repository.FindDraftByIdentifierAsync("trail");
        reloaded.Should().NotBeNull();
        var validation = new AdventureAuthoringValidator().Validate(reloaded!);
        validation.HasBlockingIssues.Should().BeFalse();
        reloaded!.DefinitionJson.Should().Contain("middle");
        reloaded.DefinitionJson.Should().Contain("token");
    }
}