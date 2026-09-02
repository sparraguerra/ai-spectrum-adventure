namespace AI.SpectrumAdventure.Application.Puzzles;

using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Puzzles;

/// <summary>Returns only declared outcomes for an accepted deterministic puzzle evaluation.</summary>
public sealed class ApplyPuzzleOutcomeUseCase
{
    public IReadOnlyCollection<PuzzleOutcomeDefinition> Execute(Puzzle puzzle, PuzzleEvaluationResult evaluation)
    {
        ArgumentNullException.ThrowIfNull(puzzle);
        ArgumentNullException.ThrowIfNull(evaluation);

        if (!evaluation.Accepted || !string.Equals(evaluation.PuzzleId, puzzle.Id.Value, StringComparison.Ordinal))
        {
            return [];
        }

        var declaredOutcomes = puzzle.Outcomes.ToDictionary(outcome => outcome.Id, StringComparer.Ordinal);
        return evaluation.ConsequenceIds
            .Where(declaredOutcomes.ContainsKey)
            .Select(consequenceId => declaredOutcomes[consequenceId])
            .ToArray();
    }
}