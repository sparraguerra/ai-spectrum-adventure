namespace AI.SpectrumAdventure.Agents;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

/// <summary>Real IAgentRunner backed by a configured Microsoft Agent Framework AIAgent, using RunAsync&lt;T&gt;
/// with a JSON-schema ResponseFormat for structured output enforcement (constitution Principle III).</summary>
public sealed class AIAgentRunner(AIAgent agent) : IAgentRunner
{
    public async Task<T> RunAsync<T>(string prompt, CancellationToken cancellationToken = default)
    {
        var options = new ChatClientAgentRunOptions(new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.ForJsonSchema<T>(),
        });

        var response = await agent.RunAsync<T>(prompt, options: options, cancellationToken: cancellationToken);
        return response.Result;
    }
}
