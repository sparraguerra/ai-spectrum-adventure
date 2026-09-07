namespace AI.SpectrumAdventure.Agents.Authoring;

using AI.SpectrumAdventure.Contracts;

public sealed class AuthoringProposalValidator
{
    public AuthoringProposalValidation Validate(AuthoringProposalContract proposal) => AuthoringProposalContractValidator.Validate(proposal);
}