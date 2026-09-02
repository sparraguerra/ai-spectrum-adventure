namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Contracts;

public interface IAuthoringProposalAgent
{
    Task<AuthoringProposalContract> CreateProposalAsync(Guid draftId, string request, string draftContext, CancellationToken cancellationToken = default);
}