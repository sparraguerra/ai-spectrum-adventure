namespace AI.SpectrumAdventure.IntegrationTests;

using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;

public sealed class E2E_InventoryTests
{
    [Fact]
    public void Inventory_TracksBridgeKeyAndAllowsItsLaterUse()
    {
        var game = E2ETestSupport.CreateStartedGame();
        AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Go, "north"));
        AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Go, "east"));

        var take = AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Take, AdventureWorldFactory.BridgeKey.Value));
    AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(IntentAction.Go, "west"));
        E2ETestSupport.GrantTowerClue(game);
        var use = AdventureOrchestrator.ProcessAction(game, E2ETestSupport.Intent(
            IntentAction.Use,
            AdventureWorldFactory.BridgeKey.Value,
            new Dictionary<string, string> { ["on"] = AdventureWorldFactory.ForgottenTowerEntrance.Value }));

        take.InventoryItemIds.Should().Contain(AdventureWorldFactory.BridgeKey.Value);
        use.Success.Should().BeTrue();
        game.Puzzle.Solved.Should().BeTrue();
    }
}