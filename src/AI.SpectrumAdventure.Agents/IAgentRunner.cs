namespace AI.SpectrumAdventure.Agents;

/// <summary>
/// The seam between our typed agent wrappers (NarratorAgent, NpcAgent, VisualArtDirectorAgent) and the concrete
/// Microsoft Agent Framework `AIAgent`. Isolating this interface keeps our agent wrappers unit-testable with a
/// fake runner (research.md Decision 6) without depending on live network access or the exact AIAgent call shape.
/// </summary>
public interface IAgentRunner
{
    /// <summary>Runs the agent with structured-output enforcement, returning the deserialized result of type T.</summary>
    Task<T> RunAsync<T>(string prompt, CancellationToken cancellationToken = default);
}
