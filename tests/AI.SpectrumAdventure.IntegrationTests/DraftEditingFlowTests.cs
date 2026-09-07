namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class DraftEditingFlowTests
{
    [Fact]
    public async Task PersistedDraftCanBeEditedAndConcurrentWriterIsRejected()
    {
        var database = Guid.NewGuid().ToString();
        await using var firstContext = TestDbContextFactory.Create(database);
        await using var secondContext = TestDbContextFactory.Create(database);
        var first = new DraftUseCases(new EfAdventureAuthoringRepository(firstContext), new PermissiveAdventureValidator());
        var second = new DraftUseCases(new EfAdventureAuthoringRepository(secondContext), new PermissiveAdventureValidator());
        var initial = new AdventureDraftRequest("editor-flow", "Draft", "start", "{}");

        var created = await first.CreateAsync(initial);
        var loadedByFirst = await first.LoadAsync("editor-flow");
        var loadedBySecond = await second.LoadAsync("editor-flow");

        var firstSave = await first.SaveAsync(loadedByFirst.Draft!.Id, loadedByFirst.Draft.Revision, initial with { Title = "First edit" });
        var staleSave = await second.SaveAsync(loadedBySecond.Draft!.Id, loadedBySecond.Draft.Revision, initial with { Title = "Stale edit" });

        created.Succeeded.Should().BeTrue();
        firstSave.Succeeded.Should().BeTrue();
        staleSave.Status.Should().Be(DraftOperationStatus.Conflict);
        (await first.LoadAsync("editor-flow")).Draft!.Title.Should().Be("First edit");
    }
}