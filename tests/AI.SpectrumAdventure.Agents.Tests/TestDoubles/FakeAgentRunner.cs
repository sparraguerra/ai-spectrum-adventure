namespace AI.SpectrumAdventure.Agents.Tests.TestDoubles;

using AI.SpectrumAdventure.Agents;

/// <summary>A scriptable IAgentRunner test double: returns queued canned responses or throws, letting tests
/// simulate schema-invalid output, transient failures, and successful structured responses without any network call.</summary>
public sealed class FakeAgentRunner : IAgentRunner
{
    private readonly Queue<Func<object>> _responses = new();

    public int CallCount { get; private set; }

    public FakeAgentRunner EnqueueResult<T>(T result) where T : notnull
    {
        _responses.Enqueue(() => result);
        return this;
    }

    public FakeAgentRunner EnqueueFailure(Exception exception)
    {
        _responses.Enqueue(() => throw exception);
        return this;
    }

    public Task<T> RunAsync<T>(string prompt, CancellationToken cancellationToken = default)
    {
        CallCount++;
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("FakeAgentRunner has no more queued responses.");
        }

        var produce = _responses.Dequeue();
        return Task.FromResult((T)produce());
    }
}
