namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Authoring;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Tests.TestDoubles;
using FluentAssertions;

public sealed class AuthoringAggregateTests
{
    [Fact]
    public void Draft_AcceptsOnlyValidatedChanges_AndIncrementsRevision()
    {
        var draft = new AdventureDraftBuilder().Build();
        var validation = new AuthoringValidationResult(draft.Id, draft.Revision, []);

        draft.Update("Edited", "start", "{\"edited\":true}", validation);

        draft.Title.Should().Be("Edited");
        draft.Revision.Should().Be(1);
        draft.LastValidation.Should().Be(validation);
    }

    [Fact]
    public void Proposal_StatusChangesAreExplicit_AndAuditEntriesRetainAction()
    {
        var draft = new AdventureDraftBuilder().Build();
        var proposal = new AuthoringProposal(AuthoringProposalId.New(), draft.Id, "location", "add room", "{}");
        proposal.Reject();

        proposal.Status.Should().Be(ProposalStatus.Rejected);
        var audit = new AuthoringAuditEntry(Guid.NewGuid(), draft.Id, AuthoringAction.ProposalRejected, DateTimeOffset.UtcNow, "author");
        audit.Action.Should().Be(AuthoringAction.ProposalRejected);
    }

    [Fact]
    public void GameSnapshot_PreservesImmutableAdventureVersionIdentity()
    {
        var versionId = AdventureVersionId.New();
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow, adventureVersionId: versionId);

        var rehydrated = Game.FromSnapshot(game.ToSnapshot());

        rehydrated.AdventureVersionId.Should().Be(versionId);
    }
}