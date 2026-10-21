namespace AI.SpectrumAdventure.Agents.Intent;

using System.Diagnostics;
using System.Text.Json;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;

public sealed class IntentInterpreterAgent(IAgentRunner agentRunner) : IIntentInterpreterAgent
{
    public async Task<ParsedIntent> InterpretAsync(
        string rawInput,
        IReadOnlyDictionary<string, string>? targetVocabulary = null,
        CancellationToken cancellationToken = default)
    {
        var targetContext = targetVocabulary ?? new Dictionary<string, string>();
        return await AgentTelemetry.TrackAsync(nameof(IntentInterpreterAgent), async () =>
        {
            var candidate = await StructuredOutputValidator.ExecuteAsync(
                ct => agentRunner.RunAsync<ParsedIntent>(BuildPrompt(rawInput, targetContext), ct),
                fallback: () => ParsedIntent.Unknown(rawInput),
                cancellationToken: cancellationToken);
            var validated = Validate(candidate, rawInput, targetContext);
            Activity.Current?.SetTag("intent.action", validated.Action.ToString());
            Activity.Current?.SetTag("intent.output_valid", validated.Action != IntentAction.Unknown || candidate.Action == IntentAction.Unknown);
            return validated;
        });
    }

    private static string BuildPrompt(string rawInput, IReadOnlyDictionary<string, string> targetVocabulary)
    {
        var actionDescriptions = IntentActionVocabulary.All.Select(entry => new
        {
            action = entry.Key.ToString(),
            synonyms = entry.Value,
        });
        var context = new
        {
            playerText = rawInput,
            supportedActions = actionDescriptions,
            knownTargets = targetVocabulary,
        };

        return """
            Interpret the player's message as exactly one supported game intent, or Unknown.
            Return only the structured ParsedIntent result. Do not narrate or claim that an action succeeded.
            Use only the listed action names and known target identifiers. Never invent an action, target, or parameter.
            Use the current context and synonym examples to resolve natural paraphrases, including articles omitted by the player.
            Return Unknown for unrelated text, mere mentions, questions about an action, quotations, hypotheticals, negations,
            or when multiple materially different actions remain plausible.
            For Go, use a canonical direction as Target. For Use, use the item as Target and an optional known target as Parameters["on"].
            For TalkTo, use the character as Target and preserve the player's intended dialogue as Parameters["utterance"] when present.
            If an action is clear but its required target is unclear, return that action with a null Target so the game can ask.
            Preserve the player's original text in RawInput. Set Confidence between 0 and 1, and use 0 for Unknown.

            Player input and available context:
            """ + Environment.NewLine + JsonSerializer.Serialize(context);
    }

    private static ParsedIntent Validate(
        ParsedIntent? candidate,
        string rawInput,
        IReadOnlyDictionary<string, string> targetVocabulary)
    {
        if (candidate is null
            || !Enum.IsDefined(candidate.Action)
            || !double.IsFinite(candidate.Confidence)
            || candidate.Confidence is < 0 or > 1
            || candidate.Parameters is null)
        {
            return ParsedIntent.Unknown(rawInput);
        }

        if (candidate.Action == IntentAction.Unknown)
        {
            return ParsedIntent.Unknown(rawInput);
        }

        var vocabulary = new WorldVocabulary(targetVocabulary);
        string? target = candidate.Target;
        if (target is not null)
        {
            if (candidate.Action == IntentAction.Go)
            {
                if (!WorldVocabulary.TryResolveDirection(target, out target))
                {
                    return ParsedIntent.Unknown(rawInput);
                }
            }
            else if (!vocabulary.TryResolveKnownTarget(target, out target))
            {
                return ParsedIntent.Unknown(rawInput);
            }
        }

        if (candidate.Action is IntentAction.Look or IntentAction.Back && target is not null)
        {
            return ParsedIntent.Unknown(rawInput);
        }

        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in candidate.Parameters)
        {
            if (key.Equals("on", StringComparison.OrdinalIgnoreCase) && candidate.Action == IntentAction.Use)
            {
                if (!vocabulary.TryResolveKnownTarget(value, out var onTarget))
                {
                    return ParsedIntent.Unknown(rawInput);
                }

                parameters["on"] = onTarget;
            }
            else if (key.Equals("utterance", StringComparison.OrdinalIgnoreCase) && candidate.Action == IntentAction.TalkTo)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    parameters["utterance"] = value;
                }
            }
            else
            {
                return ParsedIntent.Unknown(rawInput);
            }
        }

        return candidate with
        {
            Target = target,
            Parameters = parameters,
            RawInput = rawInput,
        };
    }
}
