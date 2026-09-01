namespace AI.SpectrumAdventure.Infrastructure.Storage;

using AI.SpectrumAdventure.Application.Abstractions;
using Azure.Storage.Blobs;

/// <summary>Uploads a processed retro visual to Azure Blob Storage (constitution Principle XVI: generated
/// assets live outside the application container's filesystem).</summary>
public sealed class BlobVisualAssetStore(BlobContainerClient containerClient) : IVisualAssetBlobStore
{
    public async Task<string> UploadAsync(string sceneStateKey, byte[] processedImageBytes, CancellationToken cancellationToken = default)
    {
        var blobName = $"{Uri.EscapeDataString(sceneStateKey)}.png";
        var blobClient = containerClient.GetBlobClient(blobName);

        using var stream = new MemoryStream(processedImageBytes);
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);

        return blobClient.Uri.ToString();
    }
}
