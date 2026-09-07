namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Domain.Authoring;
using FluentAssertions;

public sealed class VersionHistoryUseCaseTests
{
    [Fact]
    public async Task HistoryIsOrderedAndRestoreCopiesWithoutChangingVersion()
    {
        var repository = new MemoryAuthoringRepository();
        var source = new AdventureDraft(AdventureDraftId.New(), "history", "History", "start", "{\"version\":1}");
        var first = new AdventureVersion(AdventureVersionId.New(), source.Id, source.AdventureIdentifier, 1, source.DefinitionJson, DateTimeOffset.UtcNow);
        var second = new AdventureVersion(AdventureVersionId.New(), source.Id, source.AdventureIdentifier, 2, "{\"version\":2}", DateTimeOffset.UtcNow);
        repository.Drafts[source.Id.Value] = source;
        repository.Versions.Add(second);
        repository.Versions.Add(first);

        var useCases = new VersionHistoryUseCases(repository);
        var history = await useCases.GetHistoryAsync(source.Id);
        var result = await useCases.RestoreAsNewDraftAsync(source.Id, first.Id, "history-restored");

        history.Select(version => version.Sequence).Should().Equal(1, 2);
        result.Succeeded.Should().BeTrue();
        result.RestoredDraft.Should().NotBeNull();
        result.RestoredDraft!.Id.Should().NotBe(source.Id);
        result.RestoredDraft.DefinitionJson.Should().Be(first.DefinitionJson);
        result.RestoredDraft.AdventureIdentifier.Should().Be("history-restored");
        repository.Versions.Single(version => version.Id == first.Id).DefinitionJson.Should().Be("{\"version\":1}");
    }

    [Fact]
    public async Task RestoreRejectsAnExistingIdentifier()
    {
        var repository = new MemoryAuthoringRepository();
        var source = new AdventureDraft(AdventureDraftId.New(), "history", "History", "start", "{}");
        var existing = new AdventureDraft(AdventureDraftId.New(), "existing", "Existing", "start", "{}");
        var version = new AdventureVersion(AdventureVersionId.New(), source.Id, source.AdventureIdentifier, 1, source.DefinitionJson, DateTimeOffset.UtcNow);
        repository.Drafts[source.Id.Value] = source;
        repository.Drafts[existing.Id.Value] = existing;
        repository.Versions.Add(version);

        var result = await new VersionHistoryUseCases(repository).RestoreAsNewDraftAsync(source.Id, version.Id, "existing");

        result.Status.Should().Be(VersionHistoryOperationStatus.DuplicateIdentifier);
        result.RestoredDraft.Should().BeNull();
        repository.Drafts.Should().HaveCount(2);
    }

    private sealed class MemoryAuthoringRepository : IAdventureAuthoringRepository
    {
        public Dictionary<Guid, AdventureDraft> Drafts { get; } = [];
        public List<AdventureVersion> Versions { get; } = [];

        public Task<AdventureDraft?> FindDraftAsync(AdventureDraftId id, CancellationToken cancellationToken = default) => Task.FromResult(Drafts.GetValueOrDefault(id.Value));
        public Task<AdventureDraft?> FindDraftByIdentifierAsync(string adventureIdentifier, CancellationToken cancellationToken = default) => Task.FromResult(Drafts.Values.SingleOrDefault(draft => draft.AdventureIdentifier == adventureIdentifier));
        public Task SaveDraftAsync(AdventureDraft draft, long expectedRevision, CancellationToken cancellationToken = default) { Drafts[draft.Id.Value] = draft; return Task.CompletedTask; }
        public Task PublishAsync(AdventureDraft draft, AdventureVersion version, long expectedRevision, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersionAsync(AdventureVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AdventureVersion>> GetVersionsAsync(AdventureDraftId draftId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AdventureVersion>>(Versions.Where(version => version.DraftId == draftId).OrderBy(version => version.Sequence).ToList());
        public Task AddProposalAsync(AuthoringProposal proposal, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddAuditEntryAsync(AuthoringAuditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddPlaytestSessionAsync(PlaytestSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}