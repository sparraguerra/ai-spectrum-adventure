namespace AI.SpectrumAdventure.Agents.Tests;

using AI.SpectrumAdventure.Agents.Authoring;
using AI.SpectrumAdventure.Contracts;
using FluentAssertions;
using AI.SpectrumAdventure.Agents.Tests.TestDoubles;

public sealed class AuthoringProposalAgentTests
{
    [Fact]
    public async Task InvalidOutputRetriesAndReturnsSafeInvalidProposal()
    {
        var runner = new FakeAgentRunner()
            .EnqueueResult(new AuthoringProposalContract(Guid.Empty, Guid.Empty, "unknown", new object(), "pending"))
            .EnqueueResult(new AuthoringProposalContract(Guid.Empty, Guid.Empty, "unknown", new object(), "pending"));
        var draftId = Guid.NewGuid();

        var result = await new AuthoringProposalAgent(runner).CreateProposalAsync(draftId, "add a room", "{}" );

        result.Status.Should().Be("invalid");
        result.DraftId.Should().Be(draftId);
        runner.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task AgentFailureFallsBackWithoutThrowing()
    {
        var runner = new FakeAgentRunner().EnqueueFailure(new InvalidOperationException("offline"));
        var result = await new AuthoringProposalAgent(runner).CreateProposalAsync(Guid.NewGuid(), "request", "{}" );
        result.Status.Should().Be("invalid");
    }
}