namespace AI.SpectrumAdventure.Application.Authoring;

using System.Diagnostics;
using System.Diagnostics.Metrics;

public static class AuthoringTelemetry
{
    public static readonly ActivitySource ActivitySource = new("AI.SpectrumAdventure.Application");
    public static readonly Meter Meter = new("AI.SpectrumAdventure.Application");
    private static readonly Counter<long> OperationCounter = Meter.CreateCounter<long>("authoring.operations");
    private static readonly Histogram<double> DurationMilliseconds = Meter.CreateHistogram<double>("authoring.duration_ms");

    public static AuthoringTelemetryScope Start(string operation) => new(operation);

    public sealed class AuthoringTelemetryScope : IDisposable
    {
        private readonly string operation;
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private readonly Activity? activity;

        internal AuthoringTelemetryScope(string operation)
        {
            this.operation = operation;
            activity = ActivitySource.StartActivity($"authoring.{operation}");
            activity?.SetTag("authoring.operation", operation);
        }

        public void SetTag(string key, object? value) => activity?.SetTag(key, value);

        public void Dispose()
        {
            OperationCounter.Add(1, new KeyValuePair<string, object?>("operation", operation));
            DurationMilliseconds.Record(stopwatch.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", operation));
            activity?.SetTag("authoring.duration_ms", stopwatch.Elapsed.TotalMilliseconds);
            activity?.Dispose();
        }
    }
}