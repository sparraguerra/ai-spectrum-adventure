namespace AI.SpectrumAdventure.Agents.ImagePipeline;

using System.Threading.Channels;
using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Contracts;

/// <summary>In-process, non-Dapr work queue for image generation (research.md Decision 9). Enqueue never blocks
/// the caller — the ImageGenerationWorker drains the channel on a background thread.</summary>
public sealed class ImageGenerationQueue : IImagePipeline
{
    private readonly Channel<ImageGenerationRequest> _channel = Channel.CreateUnbounded<ImageGenerationRequest>();

    public void Enqueue(ImageGenerationRequest request) => _channel.Writer.TryWrite(request);

    public ChannelReader<ImageGenerationRequest> Reader => _channel.Reader;
}
