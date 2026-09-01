namespace AI.SpectrumAdventure.Application.Rules;

using System.Diagnostics;
using AI.SpectrumAdventure.Contracts;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;

/// <summary>Dispatches a structured ParsedIntent to the correct deterministic rule module (constitution Principle IV).
/// This is the single entry point the Application layer uses to decide whether an action is possible.</summary>
public static class RulesEngine
{
    private static readonly ActivitySource ActivitySource = new("AI.SpectrumAdventure.Application");

    public static RulesValidationResult Validate(ParsedIntent intent, Game game)
    {
        using var activity = ActivitySource.StartActivity("game.rules.validate");
        var result = intent.Action switch
        {
            IntentAction.Look => RulesValidationResult.Ok(),
            IntentAction.Examine when intent.Target is not null => ObjectRules.Validate(new ItemId(intent.Target), game),
            IntentAction.Go when intent.Target is not null => MovementRules.Validate(intent.Target, game),
            IntentAction.Take when intent.Target is not null => InventoryRules.ValidateTake(new ItemId(intent.Target), game),
            IntentAction.Open when intent.Target is not null => ObjectRules.Validate(new ItemId(intent.Target), game),
            IntentAction.Use when intent.Target is not null => ItemUsageRules.ValidateUse(new ItemId(intent.Target), ParseUseTarget(intent), game),
            IntentAction.TalkTo when intent.Target is not null => NpcPresenceRules.ValidateTalkTo(new NpcId(intent.Target), game),
            _ => RulesValidationResult.Fail(RulesFailureReason.UnknownAction),
        };

        activity?.SetTag("rules.action_type", intent.Action.ToString());
        activity?.SetTag("rules.success", result.Success);
        activity?.SetTag("rules.reason", result.Reason.ToString());
        return result;
    }

    private static ItemId? ParseUseTarget(ParsedIntent intent) =>
        intent.Parameters.TryGetValue("on", out var onTarget) ? new ItemId(onTarget) : null;
}
