namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Puzzles;
using FluentAssertions;
using Xunit;

public class PuzzleTests
{
    private static Puzzle CreatePuzzle() =>
        new(new PuzzleId("forgotten-tower-entrance"), new PuzzleSolutionCondition(new ItemId("bridge-key"), "tower-clue"));

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void CanSolve_ReturnsFalse_UnlessBothConditionsAreSatisfied(bool hasItem, bool knowsClue)
    {
        var puzzle = CreatePuzzle();

        puzzle.CanSolve(hasItem, knowsClue).Should().BeFalse();
    }

    [Fact]
    public void CanSolve_ReturnsTrue_WhenBothConditionsAreSatisfied()
    {
        var puzzle = CreatePuzzle();

        puzzle.CanSolve(hasRequiredItem: true, knowsRequiredClue: true).Should().BeTrue();
    }

    [Fact]
    public void CanSolve_ReturnsFalse_OnceAlreadySolved_EvenIfConditionsStillHold()
    {
        var puzzle = CreatePuzzle();
        puzzle.MarkSolved();

        puzzle.CanSolve(hasRequiredItem: true, knowsRequiredClue: true).Should().BeFalse();
    }

    [Fact]
    public void MarkSolved_IsIdempotent()
    {
        var puzzle = CreatePuzzle();

        puzzle.MarkSolved();
        puzzle.MarkSolved();

        puzzle.Solved.Should().BeTrue();
    }
}
