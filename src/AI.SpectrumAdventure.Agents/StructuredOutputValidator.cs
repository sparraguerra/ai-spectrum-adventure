namespace AI.SpectrumAdventure.Agents;

/// <summary>
/// Validate-once, retry-once, then fall back to a safe default (constitution Principle VIII: testable AI behavior;
/// research.md Decision 4). No agent failure or schema-invalid output is ever allowed to throw to the player.
/// </summary>
public static class StructuredOutputValidator
{
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> invokeAgent,
        Func<T> fallback,
        Action<Exception>? onAttemptFailed = null,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                return await invokeAgent(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                onAttemptFailed?.Invoke(ex);
            }
        }

        return fallback();
    }
}
