namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using FluentAssertions;
using Xunit;

/// <summary>Edge Case "Repeated Action": repeating the same event under the same state must stay consistent.</summary>
public class RepeatedActionConsistencyTests
{
    [Fact]
    public void ApplyingItemTaken_Twice_IsSafe_AndInventoryStillContainsExactlyOneCopy()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var takeEvent = new ItemTakenEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.BridgeKey, AdventureWorldFactory.OldBridge);

        game.Apply(takeEvent);
        game.Apply(takeEvent);

        game.Player.Inventory.Items.Should().ContainSingle(id => id == AdventureWorldFactory.BridgeKey);
    }

    [Fact]
    public void ApplyingPuzzleSolved_Twice_ResultsInTheSameSolvedState_AsOnce()
    {
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var solvedEvent = new PuzzleSolvedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, AdventureWorldFactory.ForgottenTowerEntrance);

        game.Apply(solvedEvent);
        game.Apply(solvedEvent);

        game.Puzzle.Solved.Should().BeTrue();
        game.HasFlag(WorldFlags.TowerUnlocked).Should().BeTrue();
    }
}
