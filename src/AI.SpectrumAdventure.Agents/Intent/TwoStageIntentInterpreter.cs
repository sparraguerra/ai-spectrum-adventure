namespace AI.SpectrumAdventure.Agents.Intent;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;

/// <summary>The two-stage interpreter research.md Decision 3 describes: try the free, deterministic pattern
/// matcher first; only fall back to the (currently keyword-based, later LLM-backed) classifier on no match.</summary>
public sealed class TwoStageIntentInterpreter : IIntentInterpreter
{
    public ValueTask<ParsedIntent> InterpretAsync(
        string rawInput,
        IReadOnlyDictionary<string, string>? targetVocabulary = null,
        CancellationToken cancellationToken = default)
    {
        var vocabulary = targetVocabulary is null ? WorldVocabulary.Empty : new WorldVocabulary(targetVocabulary);
        var matched = CommandPatternInterpreter.TryInterpret(rawInput, vocabulary);
        return ValueTask.FromResult(matched ?? KeywordFallbackClassifier.Classify(rawInput, vocabulary));
    }
}
