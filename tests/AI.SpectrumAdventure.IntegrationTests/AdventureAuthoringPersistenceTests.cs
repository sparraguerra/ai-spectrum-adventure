namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Domain.Authoring;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public sealed class AdventureAuthoringPersistenceTests
{
    [Fact]
    public async Task DraftAndVersion_RoundTrip_AndGameRetainsSourceVersion()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new EfAdventureAuthoringRepository(context);
        var draft = new AdventureDraft(AdventureDraftId.New(), "persisted", "Persisted", "start", "{}");
        await repository.SaveDraftAsync(draft, -1);
        var version = new AdventureVersion(AdventureVersionId.New(), draft.Id, draft.AdventureIdentifier, 1, draft.DefinitionJson, DateTimeOffset.UtcNow);
        await repository.AddVersionAsync(version);

        (await repository.FindDraftByIdentifierAsync("persisted")).Should().NotBeNull();
        (await repository.GetVersionsAsync(draft.Id)).Should().ContainSingle().Which.Id.Should().Be(version.Id);
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow, adventureVersionId: version.Id);
        await new EfGameRepository(context).SaveAsync(game);

        var loaded = await new EfGameRepository(context).FindAsync(game.Id);
        loaded!.AdventureVersionId.Should().Be(version.Id);
    }

    [Fact]
    public async Task DraftSave_RejectsStaleRevision()
    {
        var database = Guid.NewGuid().ToString();
        await using var firstContext = TestDbContextFactory.Create(database);
        await using var secondContext = TestDbContextFactory.Create(database);
        var first = new EfAdventureAuthoringRepository(firstContext);
        var second = new EfAdventureAuthoringRepository(secondContext);
        var draft = new AdventureDraft(AdventureDraftId.New(), "concurrent", "Concurrent", "start", "{}");
        await first.SaveDraftAsync(draft, -1);
        var firstCopy = await first.FindDraftAsync(draft.Id);
        var secondCopy = await second.FindDraftAsync(draft.Id);
        firstCopy!.Update("First", "start", "{\"first\":true}", new AuthoringValidationResult(firstCopy.Id, firstCopy.Revision, []));
        await first.SaveDraftAsync(firstCopy, 0);
        secondCopy!.Update("Second", "start", "{\"second\":true}", new AuthoringValidationResult(secondCopy.Id, secondCopy.Revision, []));

        var act = () => second.SaveDraftAsync(secondCopy, 0);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}