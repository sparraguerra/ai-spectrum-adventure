namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;

public sealed class E2E_MultiTurnConsistencyTests
{
    [Fact]
    public void FifteenTurnSession_PreservesLocationInventoryAndNarrativeFacts()
    {
        var game = E2ETestSupport.CreateStartedGame();
        var actions = new[]
        {
            E2ETestSupport.Intent(IntentAction.Look), E2ETestSupport.Intent(IntentAction.Examine, AdventureWorldFactory.Sign.Value),
            E2ETestSupport.Intent(IntentAction.Go, "north"), E2ETestSupport.Intent(IntentAction.Look),
            E2ETestSupport.Intent(IntentAction.TalkTo, AdventureWorldFactory.Hermit.Value), E2ETestSupport.Intent(IntentAction.Look),
            E2ETestSupport.Intent(IntentAction.Go, "east"), E2ETestSupport.Intent(IntentAction.Look),
            E2ETestSupport.Intent(IntentAction.Examine, AdventureWorldFactory.BridgeKey.Value), E2ETestSupport.Intent(IntentAction.Take, AdventureWorldFactory.BridgeKey.Value),
            E2ETestSupport.Intent(IntentAction.Look), E2ETestSupport.Intent(IntentAction.Go, "west"),
            E2ETestSupport.Intent(IntentAction.Look), E2ETestSupport.Intent(IntentAction.Go, "south"), E2ETestSupport.Intent(IntentAction.Look),
        };

        var results = actions.Select(action => AdventureOrchestrator.ProcessAction(game, action)).ToList();

        results.Should().OnlyContain(result => result.Success);
        game.EventHistory.Should().HaveCountGreaterThanOrEqualTo(4);
        game.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.ForestEntrance);
        game.Player.Inventory.Contains(AdventureWorldFactory.BridgeKey).Should().BeTrue();
        results[10].Narrative.Should().Contain("weathered wooden bridge");
    }
}