namespace AI.SpectrumAdventure.Domain.Tests;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Puzzles;
using FluentAssertions;

public sealed class ReusablePuzzleTests
{
    [Fact]
    public void Definition_PreservesPrerequisitesAlternativesOutcomesAndChainLinks()
    {
        var puzzle = new Puzzle(
            new PuzzleId("seal"),
            [
                new PuzzleSolutionDefinition("key", [new PuzzleCondition(PuzzleConditionType.ItemPossessed, "seal-key")]),
                new PuzzleSolutionDefinition("phrase", [new PuzzleCondition(PuzzleConditionType.ClueKnown, "seal-phrase")]),
            ],
            [new PuzzlePrerequisiteReference(PuzzleConditionType.WorldFlagSet, "gate-open")],
            [new PuzzleOutcomeDefinition("reveal-map", PuzzleOutcomeType.Clue, "tower-map")],
            [new PuzzleChainLink(new PuzzleId("tower-heart"))]);

        puzzle.State.Should().Be(PuzzleState.Discovered);
        puzzle.Prerequisites.Should().ContainSingle();
        puzzle.Solutions.Should().HaveCount(2);
        puzzle.Outcomes.Should().ContainSingle();
        puzzle.ChainLinks.Single().NextPuzzleId.Should().Be(new PuzzleId("tower-heart"));
    }

    [Fact]
    public void MarkSolved_IsIdempotent()
    {
        var puzzle = new Puzzle(new PuzzleId("seal"), [new PuzzleSolutionDefinition("key", [new PuzzleCondition(PuzzleConditionType.ItemPossessed, "seal-key")])]);

        puzzle.MarkSolved();
        puzzle.MarkSolved();

        puzzle.State.Should().Be(PuzzleState.Solved);
    }

    [Fact]
    public void CanSolve_RequiresPrerequisitesAndAcceptsAnyDeclaredSolution()
    {
        var puzzle = new Puzzle(
            new PuzzleId("seal"),
            [
                new PuzzleSolutionDefinition("key", [new PuzzleCondition(PuzzleConditionType.ItemPossessed, "seal-key")]),
                new PuzzleSolutionDefinition("phrase", [new PuzzleCondition(PuzzleConditionType.ClueKnown, "seal-phrase")]),
            ],
            [new PuzzlePrerequisiteReference(PuzzleConditionType.WorldFlagSet, "gate-open")]);

        puzzle.CanSolve(_ => false, _ => true).Should().BeFalse();
        puzzle.CanSolve(_ => true, _ => false).Should().BeFalse();
        puzzle.CanSolve(_ => true, condition => condition.ReferenceId == "seal-phrase").Should().BeTrue();
    }
}