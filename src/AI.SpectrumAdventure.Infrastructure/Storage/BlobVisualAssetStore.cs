namespace AI.SpectrumAdventure.Infrastructure.Storage;

using AI.SpectrumAdventure.Application.Abstractions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

/// <summary>Uploads a processed retro visual to Azure Blob Storage (constitution Principle XVI: generated
/// assets live outside the application container's filesystem).</summary>
public sealed class BlobVisualAssetStore : IVisualAssetBlobStore
{
    private readonly BlobContainerClient containerClient;
    private readonly bool allowPublicAccess;
    private readonly string? publicBlobEndpoint;
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private bool containerInitialized;

    public BlobVisualAssetStore(BlobContainerClient containerClient, bool allowPublicAccess = false, string? publicBlobEndpoint = null)
    {
        this.containerClient = containerClient;
        this.allowPublicAccess = allowPublicAccess;
        this.publicBlobEndpoint = publicBlobEndpoint;
    }

    public async Task<string> UploadAsync(string sceneStateKey, byte[] processedImageBytes, CancellationToken cancellationToken = default)
    {
        await EnsureContainerAsync(cancellationToken);

        var blobName = $"{Uri.EscapeDataString(sceneStateKey)}.png";
        var blobClient = containerClient.GetBlobClient(blobName);

        using var stream = new MemoryStream(processedImageBytes);
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);

        return BlobUriRewriter.RewriteForClient(blobClient.Uri.ToString(), publicBlobEndpoint);
    }

    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (containerInitialized) return;

        await initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (containerInitialized) return;

            var access = allowPublicAccess ? PublicAccessType.Blob : PublicAccessType.None;
            await containerClient.CreateIfNotExistsAsync(access, cancellationToken: cancellationToken);
            if (allowPublicAccess)
            {
                await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
            }

            containerInitialized = true;
        }
        finally
        {
            initializationLock.Release();
        }
    }
}
