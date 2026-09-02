namespace AI.SpectrumAdventure.Agents.ImagePipeline;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;
using OpenAI.Images;

/// <summary>Calls an Azure OpenAI/Foundry image deployment to render a VisualSceneSpec into a raw image
/// (research.md Decision 5). Retro styling is enforced afterward by IRetroImageProcessor, not by this stage.</summary>
public sealed class AzureOpenAiImageGenerator(ImageClient imageClient) : IImageGenerator
{
    public async Task<byte[]> GenerateAsync(VisualSceneSpec spec, CancellationToken cancellationToken = default)
    {
        var prompt = $"{spec.Style}, {spec.Mood} mood, location: {spec.LocationId}, featuring: {string.Join(", ", spec.Objects)}; visual characteristics: {string.Join(", ", spec.VisualCharacteristics ?? [])}";
        var image = await imageClient.GenerateImageAsync(prompt, cancellationToken: cancellationToken);
        return image.Value.ImageBytes.ToArray();
    }
}
