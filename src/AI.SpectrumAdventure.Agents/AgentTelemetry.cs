namespace AI.SpectrumAdventure.Agents;

using System.Diagnostics;

/// <summary>Instruments every agent invocation with a span (agent name, duration, success/failure, schema-valid
/// flag) without logging raw prompt/response content by default (constitution Principle IX).</summary>
public static class AgentTelemetry
{
    public static readonly ActivitySource ActivitySource = new("AI.SpectrumAdventure.Agents");

    public static async Task<T> TrackAsync<T>(string agentName, Func<Task<T>> operation)
    {
        using var activity = ActivitySource.StartActivity($"agent.{agentName}");
        activity?.SetTag("agent.name", agentName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await operation();
            activity?.SetTag("agent.success", true);
            activity?.SetTag("agent.schema_valid", true);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetTag("agent.success", false);
            activity?.SetTag("agent.schema_valid", false);
            activity?.SetTag("agent.error_type", ex.GetType().Name);
            throw;
        }
        finally
        {
            activity?.SetTag("agent.duration_ms", stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}
