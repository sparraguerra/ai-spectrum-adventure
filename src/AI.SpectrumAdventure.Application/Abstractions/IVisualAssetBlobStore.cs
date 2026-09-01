namespace AI.SpectrumAdventure.Application.Abstractions;

/// <summary>Stores a processed image and returns its externally-addressable URI (Azure Blob Storage in production).</summary>
public interface IVisualAssetBlobStore
{
    Task<string> UploadAsync(string sceneStateKey, byte[] processedImageBytes, CancellationToken cancellationToken = default);
}
