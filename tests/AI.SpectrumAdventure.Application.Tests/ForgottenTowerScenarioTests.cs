namespace AI.SpectrumAdventure.Application.Tests;

using AI.SpectrumAdventure.Application.Orchestration;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

/// <summary>US9: full deterministic playthrough of "The Forgotten Tower" primary puzzle.</summary>
public class ForgottenTowerScenarioTests
{
    private static ParsedIntent Intent(IntentAction action, string? target, Dictionary<string, string>? parameters = null) =>
        new(action, target, parameters ?? new Dictionary<string, string>(), Confidence: 1.0, RawInput: target ?? string.Empty);

    [Fact]
    public void Playthrough_ExploreAllLocations_ObtainKey_LearnClue_SolvePuzzle_UnlocksTowerEntrance()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);

        // Explore: Forest Entrance -> Dark Forest -> Old Bridge.
        AdventureOrchestrator.ProcessAction(game, Intent(IntentAction.Go, "North")).Success.Should().BeTrue();
        AdventureOrchestrator.ProcessAction(game, Intent(IntentAction.Go, "East")).Success.Should().BeTrue();

        // Obtain the item.
        var take = AdventureOrchestrator.ProcessAction(game, Intent(IntentAction.Take, AdventureWorldFactory.BridgeKey.Value));
        take.Success.Should().BeTrue();

        // Return to Dark Forest and learn the clue from the NPC (simulated NPC reveal — real agent arrives in Phase 11).
        AdventureOrchestrator.ProcessAction(game, Intent(IntentAction.Go, "West")).Success.Should().BeTrue();
        game.Apply(new NpcRelationshipChangedEvent(
            Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.Hermit, WorldFlags.NpcGaveClue,
            "What do you know of the tower?", "Beware the door that only opens for those who ask kindly."));
        game.Player.KnownClues.Should().Contain(AdventureWorldFactory.TowerClueKey);

        // Attempting to enter is still blocked before solving.
        AdventureOrchestrator.ProcessAction(game, Intent(IntentAction.Go, "West")).Success.Should().BeFalse();

        // Solve the puzzle: use the key targeting the tower entrance.
        var solve = AdventureOrchestrator.ProcessAction(
            game,
            Intent(IntentAction.Use, AdventureWorldFactory.BridgeKey.Value, new Dictionary<string, string> { ["on"] = AdventureWorldFactory.ForgottenTowerEntrance.Value }));

        solve.Success.Should().BeTrue();
        game.Puzzle.Solved.Should().BeTrue();

        // The entrance is now accessible and stays that way.
        AdventureOrchestrator.ProcessAction(game, Intent(IntentAction.Go, "West")).Success.Should().BeTrue();
        game.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.ForgottenTower);
    }
}
