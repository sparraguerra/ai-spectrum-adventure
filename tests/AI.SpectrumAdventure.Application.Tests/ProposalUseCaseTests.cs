namespace AI.SpectrumAdventure.Application.Tests;

using System.Text.Json;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Authoring;
using FluentAssertions;

public sealed class ProposalUseCaseTests
{
    [Fact]
    public async Task RejectAndInvalidAcceptLeaveDraftUnchanged()
    {
        var repository = new MemoryRepository();
        var draft = await SeedAsync(repository);
        var proposal = Proposal(draft, "replace", "bad-path", "value");
        await repository.AddProposalAsync(proposal);
        var useCases = new ProposalUseCases(repository, new FakeProposalAgent(), new AdventureAuthoringValidator());

        var before = draft.DefinitionJson;
        await useCases.RejectAsync(proposal);
        proposal.Status.Should().Be(ProposalStatus.Rejected);
        draft.DefinitionJson.Should().Be(before);
        repository.Audits.Should().ContainSingle(audit => audit.Action == AuthoringAction.ProposalRejected);

        var invalid = Proposal(draft, "replace", "/definition", "{\"locations\":[]}");
        await repository.AddProposalAsync(invalid);
        var result = await useCases.AcceptAsync(invalid, draft.Revision);
        result.Succeeded.Should().BeFalse();
        invalid.Status.Should().Be(ProposalStatus.Invalid);
        draft.DefinitionJson.Should().Be(before);
    }

    [Fact]
    public async Task AcceptedProposalUpdatesDraftAndAuditsExplicitAuthorAction()
    {
        var repository = new MemoryRepository();
        var draft = await SeedAsync(repository);
        var proposal = Proposal(draft, "replace", "/definition", "{\"id\":\"harbor\",\"locations\":[{\"id\":\"start\",\"exits\":[{\"direction\":\"East\",\"to\":\"room\"}]},{\"id\":\"room\",\"objectIds\":[\"key\"]}],\"items\":[{\"id\":\"key\"}]}");

        var result = await new ProposalUseCases(repository, new FakeProposalAgent(), new AdventureAuthoringValidator()).AcceptAsync(proposal, draft.Revision);

        result.Succeeded.Should().BeTrue();
        result.Draft!.Revision.Should().Be(1);
        result.Draft.DefinitionJson.Should().Contain("room");
        proposal.Status.Should().Be(ProposalStatus.Accepted);
        repository.Audits.Should().ContainSingle(audit => audit.Action == AuthoringAction.ProposalAccepted);
    }

    private static async Task<AdventureDraft> SeedAsync(MemoryRepository repository)
    {
        var draft = new AdventureDraft(AdventureDraftId.New(), "harbor", "Harbor", "start", "{\"id\":\"harbor\",\"locations\":[{\"id\":\"start\",\"exits\":[{\"direction\":\"East\",\"to\":\"room\"}]},{\"id\":\"room\"}],\"items\":[]}");
        await repository.SaveDraftAsync(draft, -1);
        return draft;
    }

    private static AuthoringProposal Proposal(AdventureDraft draft, string operation, string path, string value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value));
        var patch = new AuthoringProposalPatch([new AuthoringPatchOperation(operation, path, document.RootElement.Clone())]);
        return new AuthoringProposal(AuthoringProposalId.New(), draft.Id, "adventure", "test", JsonSerializer.Serialize(patch));
    }

    private sealed class FakeProposalAgent : IAuthoringProposalAgent
    {
        public Task<AuthoringProposalContract> CreateProposalAsync(Guid draftId, string request, string draftContext, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuthoringProposalContract(Guid.NewGuid(), draftId, "adventure", new AuthoringProposalPatch([]), "invalid"));
    }

    private sealed class MemoryRepository : IAdventureAuthoringRepository
    {
        private readonly Dictionary<Guid, AdventureDraft> drafts = [];
        public List<AuthoringAuditEntry> Audits { get; } = [];
        public Task<AdventureDraft?> FindDraftAsync(AdventureDraftId id, CancellationToken cancellationToken = default) => Task.FromResult(drafts.GetValueOrDefault(id.Value));
        public Task<AdventureDraft?> FindDraftByIdentifierAsync(string adventureIdentifier, CancellationToken cancellationToken = default) => Task.FromResult(drafts.Values.SingleOrDefault(draft => draft.AdventureIdentifier == adventureIdentifier));
        public Task SaveDraftAsync(AdventureDraft draft, long expectedRevision, CancellationToken cancellationToken = default) { drafts[draft.Id.Value] = draft; return Task.CompletedTask; }
        public Task PublishAsync(AdventureDraft draft, AdventureVersion version, long expectedRevision, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddVersionAsync(AdventureVersion version, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AdventureVersion>> GetVersionsAsync(AdventureDraftId draftId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AdventureVersion>>([]);
        public Task AddProposalAsync(AuthoringProposal proposal, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveProposalAsync(AuthoringProposal proposal, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddAuditEntryAsync(AuthoringAuditEntry entry, CancellationToken cancellationToken = default) { Audits.Add(entry); return Task.CompletedTask; }
        public Task AddPlaytestSessionAsync(PlaytestSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}