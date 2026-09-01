namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

/// <summary>
/// The Domain never decides whether an action is *valid* (that's the RulesEngine's job in Phase 3) - its
/// invariant is simply that no state changes unless a GameEvent is actually applied. These tests document
/// that guarantee so the RulesEngine can rely on it: "no proposed events" == "no state change".
/// </summary>
public class InvalidActionSafetyTests
{
    [Fact]
    public void Game_WithNoEventsApplied_StateRemainsAtInitialSnapshot()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);

        game.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.ForestEntrance);
        game.Player.Inventory.Items.Should().BeEmpty();
        game.Puzzle.Solved.Should().BeFalse();
        game.EventHistory.Should().BeEmpty();
    }

    [Fact]
    public void ApplyRange_WithEmptySequence_LeavesGameUnchanged()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);

        game.ApplyRange([]);

        game.EventHistory.Should().BeEmpty();
        game.Player.CurrentLocationId.Should().Be(AdventureWorldFactory.ForestEntrance);
    }
}
