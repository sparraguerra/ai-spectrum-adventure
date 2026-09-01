namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

public class AdventureOrchestratorTests
{
    private static ParsedIntent Intent(IntentAction action, string? target, Dictionary<string, string>? parameters = null) =>
        new(action, target, parameters ?? new Dictionary<string, string>(), Confidence: 1.0, RawInput: target ?? string.Empty);

    [Fact]
    public async Task FullDeterministicLoop_StartLookMoveTakeUseMissingItem_BehavesCorrectlyAtEachStep()
    {
        var repository = new InMemoryGameRepository();
        var game = await new StartGameUseCase(repository).ExecuteAsync();

        var look = AdventureOrchestrator.ProcessAction(game, Intent(IntentAction.Look, null));
        look.Success.Should().BeTrue();
        look.CurrentLocationId.Should().Be(AdventureWorldFactory.ForestEntrance.Value);

        var move = AdventureOrchestrator.ProcessAction(game, Intent(IntentAction.Go, "North"));
        move.Success.Should().BeTrue();
        move.CurrentLocationId.Should().Be(AdventureWorldFactory.DarkForest.Value);
        move.SceneChanged.Should().BeTrue();

        var moveToBridge = AdventureOrchestrator.ProcessAction(game, Intent(IntentAction.Go, "East"));
        moveToBridge.Success.Should().BeTrue();

        var take = AdventureOrchestrator.ProcessAction(game, Intent(IntentAction.Take, AdventureWorldFactory.BridgeKey.Value));
        take.Success.Should().BeTrue();
        take.InventoryItemIds.Should().Contain(AdventureWorldFactory.BridgeKey.Value);

        var useMissingItem = AdventureOrchestrator.ProcessAction(game, Intent(IntentAction.Use, AdventureWorldFactory.Sign.Value));
        useMissingItem.Success.Should().BeFalse();
        useMissingItem.Reason.Should().Be("ItemNotInInventory");
    }
}
