namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Domain.Authoring;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;

public sealed class StartPlaytestUseCaseTests
{
    private const string ValidDefinition = "{\"id\":\"playtest\",\"startingLocationId\":\"start\",\"locations\":[{\"id\":\"start\",\"exits\":[{\"direction\":\"East\",\"to\":\"room\"}],\"objectIds\":[\"key\"]},{\"id\":\"room\"}],\"items\":[{\"id\":\"key\"}]}";

    [Fact]
    public async Task InvalidDraftCannotStartPlaytest()
    {
        var authoring = new MemoryAuthoringRepository();
        var draft = new AdventureDraft(AdventureDraftId.New(), "playtest", "Playtest", "missing", "{\"id\":\"playtest\",\"locations\":[]}");
        authoring.Drafts[draft.Id.Value] = draft;

        var result = await new StartPlaytestUseCase(authoring, new MemoryGameRepository(), new AdventureAuthoringValidator()).ExecuteAsync(draft.Id, draft.Revision);

        result.Started.Should().BeFalse();
        result.Validation.HasBlockingIssues.Should().BeTrue();
        authoring.Sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidPlaytestUsesAnIsolatedSnapshotAndRecordsItsIdentity()
    {
        var authoring = new MemoryAuthoringRepository();
        var draft = new AdventureDraft(AdventureDraftId.New(), "playtest", "Playtest", "start", ValidDefinition);
        authoring.Drafts[draft.Id.Value] = draft;
        var games = new MemoryGameRepository();

        var result = await new StartPlaytestUseCase(authoring, games, new AdventureAuthoringValidator()).ExecuteAsync(draft.Id, draft.Revision);

        result.Started.Should().BeTrue();
        result.Game!.AdventureVersionId.Should().BeNull();
        result.Session!.DraftRevision.Should().Be(draft.Revision);
        result.Session.SnapshotHash.Should().NotBeNullOrWhiteSpace();
        result.Session.GameId.Should().Be(result.Game.Id.Value);
        draft.DefinitionJson.Should().Be(ValidDefinition);
        games.Saved.Should().ContainSingle().Which.Should().BeSameAs(result.Game);
    }

    private sealed class MemoryAuthoringRepository : IAdventureAuthoringRepository
    {
        public Dictionary<Guid, AdventureDraft> Drafts { get; } = [];
        public List<PlaytestSession> Sessions { get; } = [];
        public Task<AdventureDraft?> FindDraftAsync(AdventureDraftId id, CancellationToken cancellationToken = default) => Task.FromResult(Drafts.GetValueOrDefault(id.Value));
        public Task<AdventureDraft?> FindDraftByIdentifierAsync(string adventureIdentifier, CancellationToken cancellationToken = default) => Task.FromResult(Drafts.Values.SingleOrDefault(draft => draft.AdventureIdentifier == adventureIdentifier));
        public Task SaveDraftAsync(AdventureDraft draft, long expectedRevision, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PublishAsync(AdventureDraft draft, AdventureVersion version, long expectedRevision, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersionAsync(AdventureVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AdventureVersion>> GetVersionsAsync(AdventureDraftId draftId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AdventureVersion>>([]);
        public Task AddProposalAsync(AuthoringProposal proposal, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddAuditEntryAsync(AuthoringAuditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddPlaytestSessionAsync(PlaytestSession session, CancellationToken cancellationToken = default) { Sessions.Add(session); return Task.CompletedTask; }
    }

    private sealed class MemoryGameRepository : IGameRepository
    {
        public List<Game> Saved { get; } = [];
        public Task<Game?> FindAsync(GameId id, CancellationToken cancellationToken = default) => Task.FromResult(Saved.SingleOrDefault(game => game.Id == id));
        public Task SaveAsync(Game game, CancellationToken cancellationToken = default) { Saved.Add(game); return Task.CompletedTask; }
    }
}