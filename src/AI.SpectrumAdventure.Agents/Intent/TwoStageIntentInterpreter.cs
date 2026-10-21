namespace AI.SpectrumAdventure.Agents.Intent;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;

/// <summary>Try deterministic command patterns first, then use the bounded AI classifier for natural language.</summary>
public sealed class TwoStageIntentInterpreter(IIntentInterpreterAgent intentInterpreterAgent) : IIntentInterpreter
{
    public async ValueTask<ParsedIntent> InterpretAsync(
        string rawInput,
        IReadOnlyDictionary<string, string>? targetVocabulary = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var vocabulary = targetVocabulary is null ? WorldVocabulary.Empty : new WorldVocabulary(targetVocabulary);
        var matched = CommandPatternInterpreter.TryInterpret(rawInput, vocabulary);
        if (matched is not null)
        {
            return matched;
        }

        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return ParsedIntent.Unknown(rawInput);
        }

        return await intentInterpreterAgent.InterpretAsync(rawInput, targetVocabulary, cancellationToken);
    }
}
