namespace AI.SpectrumAdventure.Agents.Authoring;

using System.Text.Json;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;

public sealed class AuthoringProposalAgent(IAgentRunner agentRunner) : IAuthoringProposalAgent
{
    public Task<AuthoringProposalContract> CreateProposalAsync(Guid draftId, string request, string draftContext, CancellationToken cancellationToken = default) =>
        AgentTelemetry.TrackAsync(nameof(AuthoringProposalAgent), () =>
            StructuredOutputValidator.ExecuteAsync(
                async ct =>
                {
                    var proposal = await agentRunner.RunAsync<AuthoringProposalContract>(BuildPrompt(draftId, request, draftContext), ct);
                    var validation = AuthoringProposalContractValidator.Validate(proposal);
                    if (!validation.IsValid) throw new InvalidOperationException(string.Join(" ", validation.Issues));
                    return proposal;
                },
                () => new AuthoringProposalContract(Guid.NewGuid(), draftId, "adventure", new AuthoringProposalPatch([]), "invalid"),
                cancellationToken: cancellationToken));

    private static string BuildPrompt(Guid draftId, string request, string draftContext) =>
        "You are an authoring proposal agent. Return only a pending structured proposal. " +
        "Suggest JSON patch operations; never publish, persist, or directly mutate a draft. " +
        $"Draft id: {draftId}{Environment.NewLine}Author request: {request}{Environment.NewLine}" +
        $"Scoped draft context: {draftContext}";
}