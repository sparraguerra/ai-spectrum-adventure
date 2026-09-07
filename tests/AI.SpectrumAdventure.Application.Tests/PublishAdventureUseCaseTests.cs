namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Domain.Authoring;
using FluentAssertions;

public sealed class PublishAdventureUseCaseTests
{
    [Fact]
    public async Task InvalidDraftIsBlockedWithElementSpecificIssues()
    {
        var repository = new MemoryAuthoringRepository();
        var draft = new AdventureDraft(AdventureDraftId.New(), "publish-test", "Test", "missing", "{\"id\":\"publish-test\",\"locations\":[]}");
        await repository.SaveDraftAsync(draft, -1);

        var result = await new PublishAdventureUseCase(repository, new AdventureAuthoringValidator()).ExecuteAsync(draft.Id, draft.Revision);

        result.Published.Should().BeFalse();
        result.Validation.HasBlockingIssues.Should().BeTrue();
        result.Issues.Should().Contain(issue => issue.StartsWith("locations:", StringComparison.Ordinal));
        repository.Versions.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidDraftCreatesImmutableSequentialVersion()
    {
        var repository = new MemoryAuthoringRepository();
        var definition = "{\"id\":\"publish-test\",\"locations\":[{\"id\":\"start\",\"exits\":[{\"direction\":\"East\",\"to\":\"room\"}],\"objectIds\":[\"key\"]},{\"id\":\"room\"}],\"items\":[{\"id\":\"key\"}]}";
        var draft = new AdventureDraft(AdventureDraftId.New(), "publish-test", "Test", "start", definition);
        await repository.SaveDraftAsync(draft, -1);
        var publisher = new PublishAdventureUseCase(repository, new AdventureAuthoringValidator());

        var first = await publisher.ExecuteAsync(draft.Id, draft.Revision);
        var reloaded = await repository.FindDraftAsync(draft.Id);
        var second = await publisher.ExecuteAsync(draft.Id, reloaded!.Revision);

        first.Published.Should().BeTrue();
        second.Published.Should().BeTrue();
        first.Version!.Sequence.Should().Be(1);
        second.Version!.Sequence.Should().Be(2);
        first.Version.DefinitionJson.Should().Be(definition);
        first.Version.Id.Should().NotBe(second.Version.Id);
        repository.Versions.Should().HaveCount(2);
    }

    private sealed class MemoryAuthoringRepository : IAdventureAuthoringRepository
    {
        private readonly Dictionary<Guid, AdventureDraft> drafts = [];
        public List<AdventureVersion> Versions { get; } = [];
        public Task<AdventureDraft?> FindDraftAsync(AdventureDraftId id, CancellationToken cancellationToken = default) => Task.FromResult(drafts.GetValueOrDefault(id.Value));
        public Task<AdventureDraft?> FindDraftByIdentifierAsync(string adventureIdentifier, CancellationToken cancellationToken = default) => Task.FromResult(drafts.Values.SingleOrDefault(draft => draft.AdventureIdentifier == adventureIdentifier));
        public Task SaveDraftAsync(AdventureDraft draft, long expectedRevision, CancellationToken cancellationToken = default) { drafts[draft.Id.Value] = draft; return Task.CompletedTask; }
        public Task PublishAsync(AdventureDraft draft, AdventureVersion version, long expectedRevision, CancellationToken cancellationToken = default) { drafts[draft.Id.Value] = draft; Versions.Add(version); return Task.CompletedTask; }
        public Task AddVersionAsync(AdventureVersion version, CancellationToken cancellationToken = default) { Versions.Add(version); return Task.CompletedTask; }
        public Task<IReadOnlyList<AdventureVersion>> GetVersionsAsync(AdventureDraftId draftId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AdventureVersion>>(Versions.Where(version => version.DraftId == draftId).ToList());
        public Task AddProposalAsync(AuthoringProposal proposal, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddAuditEntryAsync(AuthoringAuditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddPlaytestSessionAsync(PlaytestSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
