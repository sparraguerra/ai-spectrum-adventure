namespace AI.SpectrumAdventure.Application.Orchestration;

using System.Diagnostics;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Application.Games;
using AI.SpectrumAdventure.Application.Rules;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Events;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Domain.Locations;

/// <summary>
/// Coordinates the deterministic gameplay pipeline: Intent -> RulesEngine -> Game.Apply -> ActionResult.
/// Narration here is a temporary deterministic template; Phase 10 replaces it with the Narrator agent while
/// preserving this exact validated-outcome contract (constitution Principle IV: rules before narrative).
/// </summary>
public static class AdventureOrchestrator
{
    private static readonly ActivitySource ActivitySource = new("AI.SpectrumAdventure.Application");

    /// <summary>Actions that require a resolved Target to be meaningful; a recognized action with no target is
    /// ambiguous (Edge Case: Ambiguous Action) rather than simply unknown.</summary>
    private static readonly HashSet<IntentAction> TargetRequiredActions =
    [
        IntentAction.Go, IntentAction.Examine, IntentAction.Take, IntentAction.Open, IntentAction.Use, IntentAction.TalkTo,
    ];

    public static ActionResult ProcessAction(Game game, ParsedIntent intent)
    {
        using var activity = ActivitySource.StartActivity("game.action.process");
        activity?.SetTag("game.action_type", intent.Action.ToString());

        if (intent.Action != IntentAction.Unknown && intent.Target is null && TargetRequiredActions.Contains(intent.Action))
        {
            var result = BuildResult(game, success: false, RulesFailureReason.AmbiguousIntent, BuildAmbiguousNarrative(intent), sceneChanged: false);
            activity?.SetTag("game.action_success", result.Success);
            activity?.SetTag("game.outcome", result.Reason);
            return result;
        }

        var validation = RulesEngine.Validate(intent, game);

        if (!validation.Success)
        {
            var result = BuildResult(game, success: false, validation.Reason, BuildFailureNarrative(validation.Reason), sceneChanged: false);
            activity?.SetTag("game.action_success", result.Success);
            activity?.SetTag("game.outcome", result.Reason);
            return result;
        }

        game.ApplyRange(validation.ProposedEvents);

        var sceneChanged = validation.ProposedEvents.Any(e =>
            e is PlayerMovedEvent or PuzzleSolvedEvent or LocationDiscoveredEvent or ObjectStateChangedEvent);

        var successResult = BuildResult(game, success: true, RulesFailureReason.None, BuildSuccessNarrative(intent, game), sceneChanged);
        activity?.SetTag("game.action_success", successResult.Success);
        activity?.SetTag("game.scene_changed", sceneChanged);
        return successResult;
    }

    /// <summary>
    /// Runs the same deterministic pipeline as <see cref="ProcessAction"/>, then asks the Narrator agent to
    /// enrich the narrative in-tone. On any Narrator failure/invalid output, the deterministic templated
    /// narrative from <see cref="ProcessAction"/> is used unchanged (constitution Principle IV/XXII: the
    /// gameplay loop is never blocked or corrupted by an AI failure).
    /// </summary>
    public static async Task<ActionResult> ProcessActionWithNarrationAsync(
        Game game,
        ParsedIntent intent,
        INarratorAgent narratorAgent,
        CancellationToken cancellationToken = default)
    {
        var deterministicResult = ProcessAction(game, intent);
        var narratorContext = BuildNarratorContext(game, deterministicResult);
        var narrationResult = await narratorAgent.NarrateAsync(narratorContext, cancellationToken);

        return deterministicResult with { Narrative = narrationResult.Narration };
    }

    private static NarratorContext BuildNarratorContext(Game game, ActionResult result)
    {
        var location = game.CurrentLocation;
        return new NarratorContext(
            location.Name,
            location.BaseDescription,
            VisibleObjectNames: [.. location.ObjectIds.Select(game.GetItem).Where(i => i.LocationId == location.Id && i.IsRevealed(game.WorldFlagKeys)).Select(i => i.Name)],
            PresentCharacterNames: [.. location.NpcIds.Select(game.GetNpc).Select(n => n.Name)],
            AvailableExits: [.. GetAvailableExits(location, game)],
            result.Success,
            result.Reason,
            result.Narrative);
    }

    private static IEnumerable<string> GetAvailableExits(Location location, Game game) =>
        location.Exits
            .Where(exit => exit.RequiredCondition is null || exit.RequiredCondition.IsSatisfiedBy(game.WorldFlagKeys))
            .Select(exit => exit.Direction);

    private static ActionResult BuildResult(Game game, bool success, RulesFailureReason reason, string narrative, bool sceneChanged) =>
        new(
            game.Id.Value,
            success,
            success ? null : reason.ToString(),
            narrative,
            sceneChanged,
            game.CurrentLocation.Id.Value,
            GetInventoryQuery.Execute(game),
            VisualAssetUrl: null);

    private static string BuildSuccessNarrative(ParsedIntent intent, Game game) =>
        intent.Action switch
        {
            IntentAction.Look => GetLocationDescriptionQuery.Execute(game),
            IntentAction.Go => GetLocationDescriptionQuery.Execute(game),
            IntentAction.Take => $"You take the {game.GetItem(new ItemId(intent.Target!)).Name}.",
            IntentAction.Examine => game.GetItem(new ItemId(intent.Target!)).Description,
            IntentAction.Open => game.GetItem(new ItemId(intent.Target!)).Description,
            IntentAction.Use when game.Puzzle.Solved => "With a heavy groan, the tower door swings open.",
            IntentAction.Use => "Nothing happens... yet.",
            IntentAction.TalkTo => $"{game.GetNpc(new NpcId(intent.Target!)).Name} regards you silently for now.",
            _ => "You do that.",
        };

    /// <summary>FR/Edge Case "Ambiguous Action": ask a natural in-world clarifying question instead of guessing wrong.</summary>
    private static string BuildAmbiguousNarrative(ParsedIntent intent) =>
        intent.Action switch
        {
            IntentAction.Go => "Which direction do you want to go?",
            IntentAction.Examine => "What do you want to examine?",
            IntentAction.Take => "What do you want to take?",
            IntentAction.Open => "What do you want to open?",
            IntentAction.Use => "What do you want to use?",
            IntentAction.TalkTo => "Who do you want to talk to?",
            _ => "Could you clarify what you mean?",
        };

    private static string BuildFailureNarrative(RulesFailureReason reason) =>
        reason switch
        {
            RulesFailureReason.NoSuchExit => "There is no path in that direction.",
            RulesFailureReason.ExitConditionNotMet => "Something is blocking your way. It will not budge.",
            RulesFailureReason.TargetNotPresent => "You don't see anything like that here.",
            RulesFailureReason.ItemNotInInventory => "You don't have that.",
            RulesFailureReason.AlreadyInState => "You already have that.",
            RulesFailureReason.PuzzleConditionNotMet => "Nothing happens. Something is still missing.",
            RulesFailureReason.ImpossibleAction => "That doesn't seem possible right now.",
            RulesFailureReason.UnknownAction => "You're not sure how to do that.",
            _ => "Nothing happens.",
        };
}
