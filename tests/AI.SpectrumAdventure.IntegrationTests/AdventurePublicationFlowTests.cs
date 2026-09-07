namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class AdventurePublicationFlowTests
{
    [Fact]
    public async Task InvalidThenValidPublicationExposesLatestAndPinsExplicitVersion()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new EfAdventureAuthoringRepository(context);
        var publisher = new PublishAdventureUseCase(repository, new AdventureAuthoringValidator());
        var draft = new AI.SpectrumAdventure.Domain.Authoring.AdventureDraft(
            AI.SpectrumAdventure.Domain.Authoring.AdventureDraftId.New(), "publication-flow", "Publication Flow", "start",
            "{\"id\":\"publication-flow\",\"locations\":[]}");
        await repository.SaveDraftAsync(draft, -1);

        var invalid = await publisher.ExecuteAsync(draft.Id, draft.Revision);
        invalid.Published.Should().BeFalse();
        (await repository.GetVersionsAsync(draft.Id)).Should().BeEmpty();

        var validDefinition = "{\"id\":\"publication-flow\",\"title\":\"Publication Flow\",\"locations\":[{\"id\":\"start\",\"exits\":[{\"direction\":\"East\",\"to\":\"room\"}],\"objectIds\":[\"key\"]},{\"id\":\"room\"}],\"items\":[{\"id\":\"key\"}]}";
        var valid = new AI.SpectrumAdventure.Domain.Authoring.AdventureDraft(draft.Id, draft.AdventureIdentifier, draft.Title, draft.StartingLocationId, validDefinition, draft.Revision);
        valid.RecordValidation(new AdventureAuthoringValidator().Validate(valid));
        await repository.SaveDraftAsync(valid, draft.Revision);

        var first = await publisher.ExecuteAsync(valid.Id, valid.Revision);
        first.Published.Should().BeTrue();
        var firstVersion = first.Version!;
        var changed = await repository.FindDraftAsync(valid.Id);
        var secondDraft = new AI.SpectrumAdventure.Domain.Authoring.AdventureDraft(changed!.Id, changed.AdventureIdentifier, changed.Title, changed.StartingLocationId, validDefinition.Replace("room", "new-room", StringComparison.Ordinal), changed.Revision);
        await repository.SaveDraftAsync(secondDraft, changed.Revision);
        var second = await publisher.ExecuteAsync(secondDraft.Id, secondDraft.Revision);
        second.Published.Should().BeTrue();

        var catalog = new DatabaseAdventureCatalog(context, "missing-adventures");
        var latest = await catalog.GetDefinitionAsync("publication-flow");
        var explicitFirst = await catalog.GetDefinitionAsync("publication-flow", firstVersion.Id);

        latest.VersionId!.Value.Value.Should().Be(second.VersionId!.Value);
        latest.DefinitionJson.Should().Contain("new-room");
        explicitFirst.VersionId!.Value.Value.Should().Be(firstVersion.Id.Value);
        explicitFirst.DefinitionJson.Should().Contain("\"room\"").And.NotContain("new-room");
    }
}
