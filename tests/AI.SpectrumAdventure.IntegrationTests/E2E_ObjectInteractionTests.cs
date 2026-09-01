namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;

public sealed class E2E_ObjectInteractionTests
{
    [Fact]
    public void Examine_ReflectsCurrentWorldState()
    {
        var game = E2ETestSupport.CreateStartedGame();

        var visibleItem = AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Examine, AdventureWorldFactory.Sign.Value));
        var unavailableItem = AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Examine, AdventureWorldFactory.BridgeKey.Value));

        visibleItem.Success.Should().BeTrue();
        visibleItem.Narrative.Should().Contain("weathered wooden sign");
        unavailableItem.Success.Should().BeFalse();
        unavailableItem.Reason.Should().Be("TargetNotPresent");
    }
}