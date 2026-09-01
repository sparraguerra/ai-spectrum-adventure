namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Contracts;

/// <summary>The LLM-backed fallback classifier TwoStageIntentInterpreter calls when the deterministic
/// CommandPatternInterpreter finds no match (research.md Decision 3). Never mutates GameState.</summary>
public interface IIntentInterpreterAgent
{
    Task<ParsedIntent> InterpretAsync(string rawInput, CancellationToken cancellationToken = default);
}
