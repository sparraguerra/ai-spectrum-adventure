namespace AI.SpectrumAdventure.Application.Rules;

using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Puzzles;
using AI.SpectrumAdventure.Domain.Worlds;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Application.Worlds;

/// <summary>FR-018/FR-019/FR-020: gates the Forgotten Tower entrance behind the dual item+clue condition, via Puzzle.CanSolve.</summary>
public static class PuzzleRules
{
    public static RulesValidationResult ValidateSolve(ItemId itemBeingUsed, Game game)
        => ValidateSolve(itemBeingUsed, game.Puzzle.Id, game);

    public static RulesValidationResult ValidateSolve(ItemId itemBeingUsed, PuzzleId puzzleId, Game game)
    {
        var puzzle = game.GetPuzzle(puzzleId);
        var evaluation = Evaluate(puzzle, itemBeingUsed, game, world: null);
        if (!evaluation.Accepted)
        {
            return RulesValidationResult.Fail(RulesFailureReason.PuzzleConditionNotMet);
        }

        var now = DateTimeOffset.UtcNow;
        return RulesValidationResult.Ok(
            new ItemUsedEvent(Guid.NewGuid(), now, itemBeingUsed, null),
            new PuzzleSolvedEvent(Guid.NewGuid(), now, puzzle.Id));
    }

    public static PuzzleEvaluationResult Evaluate(ParsedIntent intent, Game game, World? world = null)
    {
        using var activity = WorldTelemetry.Start("puzzle.evaluation");
        if (!intent.Parameters.TryGetValue("on", out var targetId)) return Rejected(game.Puzzle, "The action does not target this puzzle.");
        Puzzle puzzle;
        try { puzzle = game.GetPuzzle(new PuzzleId(targetId)); }
        catch (KeyNotFoundException) { return Rejected(game.Puzzle, "The action does not target this puzzle."); }
        if (intent.Action != IntentAction.Use || intent.Target is null ||
            !string.Equals(targetId, puzzle.Id.Value, StringComparison.OrdinalIgnoreCase))
        {
            return Rejected(puzzle, "The action does not target this puzzle.");
        }

        var result = Evaluate(puzzle, new ItemId(intent.Target), game, world);
        activity?.SetTag("world.puzzle_accepted", result.Accepted);
        return result;
    }

    private static PuzzleEvaluationResult Evaluate(Puzzle puzzle, ItemId itemBeingUsed, Game game, World? world)
    {
        if (puzzle.Solved)
        {
            return Rejected(puzzle, "The puzzle is already solved.");
        }

        var conditionSatisfied = (PuzzleCondition condition) => condition.Type switch
        {
            PuzzleConditionType.ItemPossessed => itemBeingUsed.Value == condition.ReferenceId && game.Player.Inventory.Contains(new ItemId(condition.ReferenceId)),
            PuzzleConditionType.ClueKnown => game.Player.KnownClues.Contains(condition.ReferenceId),
            PuzzleConditionType.WorldFlagSet => game.HasFlag(condition.ReferenceId),
            PuzzleConditionType.PuzzleSolved => IsPuzzleSolved(condition.ReferenceId, game, world),
            _ => false,
        };

        if (puzzle.Prerequisites.Any(prerequisite => !conditionSatisfied(new PuzzleCondition(prerequisite.Type, prerequisite.ReferenceId))) ||
            !puzzle.Solutions.Any(solution => solution.IsSatisfiedBy(conditionSatisfied)))
        {
            return Rejected(puzzle, "The declared puzzle conditions are not met.");
        }

        return new PuzzleEvaluationResult(
            puzzle.Id.Value,
            Accepted: true,
            puzzle.State.ToString(),
            PuzzleState.Solved.ToString(),
            [.. puzzle.Outcomes.Select(outcome => outcome.Id)],
            RejectionReason: null);
    }

    private static bool IsPuzzleSolved(string puzzleId, Game game, World? world) =>
        game.Puzzles.Any(puzzle => string.Equals(puzzle.Id.Value, puzzleId, StringComparison.OrdinalIgnoreCase) && puzzle.Solved) ||
        world?.PuzzleStates.TryGetValue(new PuzzleId(puzzleId), out var isSolved) == true && isSolved;

    private static PuzzleEvaluationResult Rejected(Puzzle puzzle, string reason) =>
        new(puzzle.Id.Value, Accepted: false, puzzle.State.ToString(), puzzle.State.ToString(), [], reason);
}
