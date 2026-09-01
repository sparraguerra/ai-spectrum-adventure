namespace AI.SpectrumAdventure.Application.Rules;

using AI.SpectrumAdventure.Domain.Events;

public enum RulesFailureReason
{
    None,
    ItemNotInInventory,
    NoSuchExit,
    ExitConditionNotMet,
    TargetNotPresent,
    AlreadyInState,
    PuzzleConditionNotMet,
    AmbiguousIntent,
    UnknownAction,
    ImpossibleAction,
}

/// <summary>The structured outcome of RulesEngine validation (constitution Principle III/IV). A Success result
/// carries the GameEvents the orchestrator may now apply; a failure carries no events and a Reason for the UI/Narrator.</summary>
public sealed record RulesValidationResult(bool Success, RulesFailureReason Reason, IReadOnlyCollection<GameEvent> ProposedEvents)
{
    public static RulesValidationResult Ok(params GameEvent[] events) => new(true, RulesFailureReason.None, events);

    public static RulesValidationResult Ok(IReadOnlyCollection<GameEvent> events) => new(true, RulesFailureReason.None, events);

    public static RulesValidationResult Fail(RulesFailureReason reason) => new(false, reason, []);
}
