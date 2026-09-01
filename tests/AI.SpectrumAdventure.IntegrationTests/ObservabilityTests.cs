namespace AI.SpectrumAdventure.IntegrationTests;

using System.Collections.Concurrent;
using System.Diagnostics;
using AI.SpectrumAdventure.Agents;
using AI.SpectrumAdventure.Domain.Common;
using AI.SpectrumAdventure.Domain.Games;
using AI.SpectrumAdventure.Infrastructure.Persistence;
using FluentAssertions;

public sealed class ObservabilityTests
{
    [Fact]
    public async Task AgentAndPersistenceFailures_EmitSafeTraceMetadata()
    {
        var activities = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is "AI.SpectrumAdventure.Agents" or "AI.SpectrumAdventure.Infrastructure",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => activities.Enqueue(activity),
        };

        ActivitySource.AddActivityListener(listener);
        AgentTelemetry.ActivitySource.HasListeners().Should().BeTrue();

        var agentAction = async () => await AgentTelemetry.TrackAsync<string>(
            "Narrator",
            () => Task.FromException<string>(new InvalidOperationException("raw prompt must not be traced")));
        await agentAction.Should().ThrowAsync<InvalidOperationException>();

        var context = TestDbContextFactory.Create();
        var repository = new EfGameRepository(context);
        await context.DisposeAsync();
        var game = AdventureWorldFactory.CreateNewGame(GameId.New(), DateTimeOffset.UtcNow);
        var persistenceAction = async () => await repository.SaveAsync(game);
        await persistenceAction.Should().ThrowAsync<ObjectDisposedException>();

        var completed = activities.ToArray();
        var agentActivity = completed.Last(activity =>
            activity.OperationName == "agent.Narrator" &&
            activity.GetTagItem("agent.error_type")?.ToString() == nameof(InvalidOperationException));
        var persistenceActivity = completed.Last(activity =>
            activity.OperationName == "persistence.game.save" &&
            activity.GetTagItem("persistence.error_type")?.ToString() == nameof(ObjectDisposedException));

        agentActivity.GetTagItem("agent.success").Should().Be(false);
        agentActivity.GetTagItem("agent.error_type").Should().Be(nameof(InvalidOperationException));
        agentActivity.Tags.Select(tag => tag.Key).Should().NotContain(key => key.Contains("prompt", StringComparison.OrdinalIgnoreCase));
        persistenceActivity.GetTagItem("persistence.success").Should().Be(false);
        persistenceActivity.GetTagItem("persistence.error_type").Should().Be(nameof(ObjectDisposedException));
    }
}