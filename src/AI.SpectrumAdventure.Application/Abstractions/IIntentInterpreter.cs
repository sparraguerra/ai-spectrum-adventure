namespace AI.SpectrumAdventure.Application.Abstractions;

using AI.SpectrumAdventure.Contracts;

/// <summary>The single entry point the orchestrator uses to turn player text into a ParsedIntent (spec FR-004/FR-005).
/// Implementations never mutate GameState — only the RulesEngine decides whether the resulting intent is feasible.</summary>
public interface IIntentInterpreter
{
    ValueTask<ParsedIntent> InterpretAsync(string rawInput, CancellationToken cancellationToken = default);
}
