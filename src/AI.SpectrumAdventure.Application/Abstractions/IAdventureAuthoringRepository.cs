namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Domain.Authoring;

public interface IAdventureAuthoringRepository
{
    Task<AdventureDraft?> FindDraftAsync(AdventureDraftId id, CancellationToken cancellationToken = default);
    Task<AdventureDraft?> FindDraftByIdentifierAsync(string adventureIdentifier, CancellationToken cancellationToken = default);
    Task SaveDraftAsync(AdventureDraft draft, long expectedRevision, CancellationToken cancellationToken = default);
    Task PublishAsync(AdventureDraft draft, AdventureVersion version, long expectedRevision, CancellationToken cancellationToken = default);
    Task AddVersionAsync(AdventureVersion version, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdventureVersion>> GetVersionsAsync(AdventureDraftId draftId, CancellationToken cancellationToken = default);
    Task AddProposalAsync(AuthoringProposal proposal, CancellationToken cancellationToken = default);
    Task<AuthoringProposal?> FindProposalAsync(AuthoringProposalId id, CancellationToken cancellationToken = default) => Task.FromResult<AuthoringProposal?>(null);
    Task SaveProposalAsync(AuthoringProposal proposal, CancellationToken cancellationToken = default) => AddProposalAsync(proposal, cancellationToken);
    Task AddAuditEntryAsync(AuthoringAuditEntry entry, CancellationToken cancellationToken = default);
    Task AddPlaytestSessionAsync(PlaytestSession session, CancellationToken cancellationToken = default);
}