namespace AI.SpectrumAdventure.Domain.Puzzles;

using AI.SpectrumAdventure.Domain.Common;

/// <summary>A deterministic puzzle definition and its authoritative state.</summary>
public sealed class Puzzle
{
    public PuzzleId Id { get; }
    public PuzzleSolutionCondition SolutionCondition { get; }
    public PuzzleState State { get; private set; }
    public IReadOnlyCollection<PuzzlePrerequisiteReference> Prerequisites { get; }
    public IReadOnlyCollection<PuzzleSolutionDefinition> Solutions { get; }
    public IReadOnlyCollection<PuzzleOutcomeDefinition> Outcomes { get; }
    public IReadOnlyCollection<PuzzleChainLink> ChainLinks { get; }
    public bool Solved => State == PuzzleState.Solved;

    public ItemId RequiredItemId => SolutionCondition.RequiredItemId;
    public string RequiredClueKey => SolutionCondition.RequiredClueKey;

    public Puzzle(PuzzleId id, PuzzleSolutionCondition solutionCondition)
        : this(
            id,
            [
                new PuzzleSolutionDefinition(
                    "legacy-item-and-clue",
                    [
                        new PuzzleCondition(PuzzleConditionType.ItemPossessed, solutionCondition.RequiredItemId.Value),
                        new PuzzleCondition(PuzzleConditionType.ClueKnown, solutionCondition.RequiredClueKey),
                    ]),
            ],
            null,
            null,
            null,
            PuzzleState.Discovered,
            solutionCondition)
    {
    }

    public Puzzle(
        PuzzleId id,
        IEnumerable<PuzzleSolutionDefinition> solutions,
        IEnumerable<PuzzlePrerequisiteReference>? prerequisites = null,
        IEnumerable<PuzzleOutcomeDefinition>? outcomes = null,
        IEnumerable<PuzzleChainLink>? chainLinks = null,
        PuzzleState state = PuzzleState.Discovered,
        PuzzleSolutionCondition? legacySolutionCondition = null)
    {
        Id = id;
        Solutions = solutions?.ToArray() ?? throw new ArgumentNullException(nameof(solutions));
        if (Solutions.Count == 0) throw new ArgumentException("A puzzle needs at least one explicit solution.", nameof(solutions));
        if (Solutions.Any(solution => string.IsNullOrWhiteSpace(solution.Id) || solution.Conditions.Count == 0)) throw new ArgumentException("Each puzzle solution needs an identifier and conditions.", nameof(solutions));
        if (Solutions.Select(solution => solution.Id).Distinct(StringComparer.Ordinal).Count() != Solutions.Count) throw new ArgumentException("Puzzle solution identifiers must be unique.", nameof(solutions));

        Prerequisites = prerequisites?.ToArray() ?? [];
        Outcomes = outcomes?.ToArray() ?? [];
        ChainLinks = chainLinks?.ToArray() ?? [];
        State = state;
        var itemCondition = Solutions.SelectMany(solution => solution.Conditions).FirstOrDefault(condition => condition.Type == PuzzleConditionType.ItemPossessed);
        var clueCondition = Solutions.SelectMany(solution => solution.Conditions).FirstOrDefault(condition => condition.Type == PuzzleConditionType.ClueKnown);
        SolutionCondition = legacySolutionCondition ?? new PuzzleSolutionCondition(
            new ItemId(itemCondition?.ReferenceId ?? string.Empty),
            clueCondition?.ReferenceId ?? string.Empty);
    }

    /// <summary>Pure, independently-testable gate (FR-019/FR-020): never solvable twice, never solvable by any other means.</summary>
    public bool CanSolve(bool hasRequiredItem, bool knowsRequiredClue) =>
        !Solved && Solutions.Any(solution => solution.IsSatisfiedBy(condition => condition.Type switch
        {
            PuzzleConditionType.ItemPossessed => hasRequiredItem && condition.ReferenceId == RequiredItemId.Value,
            PuzzleConditionType.ClueKnown => knowsRequiredClue && condition.ReferenceId == RequiredClueKey,
            _ => false,
        }));

    public bool CanSolve(Func<PuzzlePrerequisiteReference, bool> isPrerequisiteSatisfied, Func<PuzzleCondition, bool> isConditionSatisfied) =>
        !Solved && Prerequisites.All(isPrerequisiteSatisfied) && Solutions.Any(solution => solution.IsSatisfiedBy(isConditionSatisfied));

    /// <summary>Idempotency guard (T055): applying PuzzleSolved a second time has no additional effect.</summary>
    internal void MarkSolved() => State = PuzzleState.Solved;
}
