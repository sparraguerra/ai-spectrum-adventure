namespace AI.SpectrumAdventure.Application.Authoring;

using System.Text;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;

public sealed record AdventurePreviewResult(
    VisualSceneSpec Scene,
    byte[]? ImageBytes,
    string FactualSummary,
    bool UsedFallback);

public sealed class AuthoringPreviewService(
    IVisualArtDirector visualArtDirector,
    IImageGenerator imageGenerator,
    IRetroImageProcessor retroImageProcessor)
{
    public async Task<AdventurePreviewResult> CreateAsync(VisualContext context, CancellationToken cancellationToken = default)
    {
        using var telemetry = AuthoringTelemetry.Start("preview");
        VisualSceneSpec scene;
        var usedFallback = false;
        try
        {
            scene = await visualArtDirector.DescribeSceneAsync(context, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            usedFallback = true;
            telemetry.SetTag("preview.failure_type", exception.GetType().Name);
            scene = CreateFallbackScene(context);
        }

        byte[]? image = null;
        try
        {
            image = retroImageProcessor.Process(await imageGenerator.GenerateAsync(scene, cancellationToken));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            usedFallback = true;
            telemetry.SetTag("preview.image_failure_type", exception.GetType().Name);
        }

        return new AdventurePreviewResult(scene, image, CreateFactualSummary(context), usedFallback);
    }

    private static VisualSceneSpec CreateFallbackScene(VisualContext context) => new(
        context.LocationId,
        context.SceneStateKey,
        null,
        context.VisibleObjectNames.Take(12).ToArray(),
        context.Mood,
        "zx-spectrum-8-bit",
        context.VisualCharacteristics);

    private static string CreateFactualSummary(VisualContext context)
    {
        var objects = context.VisibleObjectNames.Count == 0 ? "none" : string.Join(", ", context.VisibleObjectNames);
        return new StringBuilder()
            .Append("Location: ").Append(context.LocationName)
            .Append(" | Mood: ").Append(context.Mood)
            .Append(" | Visible objects: ").Append(objects)
            .ToString();
    }
}