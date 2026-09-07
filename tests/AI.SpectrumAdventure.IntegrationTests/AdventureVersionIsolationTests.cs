namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Authoring;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class AdventureVersionIsolationTests
{
    [Fact]
    public async Task LaterPublicationDoesNotChangeActiveGameAndRestoreCreatesDraft()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new EfAdventureAuthoringRepository(context);
        var publisher = new PublishAdventureUseCase(repository, new AdventureAuthoringValidator());
        var source = new AdventureDraft(AdventureDraftId.New(), "isolation", "Isolation", "start", Definition("room"));
        await repository.SaveDraftAsync(source, -1);

        var first = await publisher.ExecuteAsync(source.Id, source.Revision);
        first.Published.Should().BeTrue();
        var firstVersion = first.Version!;
        var activeGame = AdventureWorldFactory.CreateNewGameFromJson(GameId.New(), DateTimeOffset.UtcNow, firstVersion.DefinitionJson, firstVersion.Id);

        var current = await repository.FindDraftAsync(source.Id);
        var changed = new AdventureDraft(current!.Id, current.AdventureIdentifier, current.Title, current.StartingLocationId, Definition("new-room"), current.Revision);
        await repository.SaveDraftAsync(changed, current.Revision);
        var second = await publisher.ExecuteAsync(changed.Id, changed.Revision);

        second.Published.Should().BeTrue();
        activeGame.AdventureVersionId.Should().Be(firstVersion.Id);
        var versions = await repository.GetVersionsAsync(source.Id);
        versions.Should().HaveCount(2);
        versions.Single(version => version.Id == firstVersion.Id).DefinitionJson.Should().Contain("room").And.NotContain("new-room");

        var history = new VersionHistoryUseCases(repository);
        var restored = await history.RestoreAsNewDraftAsync(source.Id, firstVersion.Id, "isolation-restored");

        restored.Succeeded.Should().BeTrue();
        restored.RestoredDraft!.DefinitionJson.Should().Be(firstVersion.DefinitionJson);
        (await repository.FindDraftByIdentifierAsync("isolation-restored")).Should().NotBeNull();
    }

    private static string Definition(string destination) =>
        $"{{\"id\":\"isolation\",\"startingLocationId\":\"start\",\"locations\":[{{\"id\":\"start\",\"exits\":[{{\"direction\":\"East\",\"to\":\"{destination}\"}}],\"objectIds\":[\"key\"]}},{{\"id\":\"{destination}\"}}],\"items\":[{{\"id\":\"key\"}}]}}";
}