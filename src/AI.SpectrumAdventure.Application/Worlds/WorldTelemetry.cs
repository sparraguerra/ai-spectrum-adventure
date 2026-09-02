namespace AI.SpectrumAdventure.Application.Worlds;

using System.Diagnostics;

public static class WorldTelemetry
{
    public static readonly ActivitySource ActivitySource = new("AI.SpectrumAdventure.Application");

    public static Activity? Start(string operation) => ActivitySource.StartActivity($"world.{operation}");
}