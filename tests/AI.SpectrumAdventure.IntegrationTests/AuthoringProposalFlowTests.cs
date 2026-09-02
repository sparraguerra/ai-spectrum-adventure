namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Authoring;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Authoring;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class AuthoringProposalFlowTests
{
    [Fact]
    public async Task AcceptRejectAndFailurePersistReviewStateWithoutUnauthorizedMutation()
    {
        await using var context = TestDbContextFactory.Create();
        var repository = new EfAdventureAuthoringRepository(context);
        var draft = new AdventureDraft(AdventureDraftId.New(), "proposal-flow", "Proposal Flow", "start", "{\"id\":\"proposal-flow\",\"locations\":[{\"id\":\"start\",\"exits\":[{\"direction\":\"East\",\"to\":\"room\"}]},{\"id\":\"room\",\"objectIds\":[\"key\"]}],\"items\":[{\"id\":\"key\"}]}");
        await repository.SaveDraftAsync(draft, -1);
        var useCases = new ProposalUseCases(repository, new ScriptedProposalAgent(), new AdventureAuthoringValidator());

        var requested = await useCases.RequestAsync(draft.Id, "make the room brighter");
        requested.Succeeded.Should().BeTrue();
        var pending = await repository.FindProposalAsync(requested.Proposal!.Id);
        pending!.Status.Should().Be(ProposalStatus.Pending);

        var accepted = await useCases.AcceptAsync(pending, draft.Revision);
        accepted.Succeeded.Should().BeTrue();
        (await repository.FindDraftAsync(draft.Id))!.Revision.Should().Be(1);
        (await repository.FindProposalAsync(pending.Id))!.Status.Should().Be(ProposalStatus.Accepted);
        context.AuthoringAuditEntries.Count().Should().Be(1);

        var rejected = new AuthoringProposal(AuthoringProposalId.New(), draft.Id, "adventure", "reject", pending.PatchJson);
        await repository.AddProposalAsync(rejected);
        await useCases.RejectAsync(rejected);
        (await repository.FindProposalAsync(rejected.Id))!.Status.Should().Be(ProposalStatus.Rejected);
        context.AuthoringAuditEntries.Count().Should().Be(2);

        var failed = await new ProposalUseCases(repository, new FailedProposalAgent(), new AdventureAuthoringValidator()).RequestAsync(draft.Id, "offline request");
        failed.Status.Should().Be(ProposalOperationStatus.Invalid);
        (await repository.FindDraftAsync(draft.Id))!.Revision.Should().Be(1);
    }

    private sealed class ScriptedProposalAgent : IAuthoringProposalAgent
    {
        public Task<AuthoringProposalContract> CreateProposalAsync(Guid draftId, string request, string draftContext, CancellationToken cancellationToken = default)
        {
            var definition = "{\"id\":\"proposal-flow\",\"locations\":[{\"id\":\"start\",\"exits\":[{\"direction\":\"East\",\"to\":\"room\"}]},{\"id\":\"room\",\"objectIds\":[\"key\"]}],\"items\":[{\"id\":\"key\"}]}";
            var patch = new AuthoringProposalPatch([new AuthoringPatchOperation("replace", "/definition", System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(definition)).RootElement.Clone())]);
            return Task.FromResult(new AuthoringProposalContract(Guid.NewGuid(), draftId, "adventure", patch, "pending"));
        }
    }

    private sealed class FailedProposalAgent : IAuthoringProposalAgent
    {
        public Task<AuthoringProposalContract> CreateProposalAsync(Guid draftId, string request, string draftContext, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuthoringProposalContract(Guid.Empty, draftId, "adventure", new object(), "invalid"));
    }
}