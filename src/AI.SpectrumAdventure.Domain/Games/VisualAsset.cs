namespace AI.SpectrumAdventure.Domain.Games;

public enum VisualAssetStatus
{
    Pending,
    Ready,
    Failed,
}

/// <summary>Read model tracking one generated (or in-progress) retro visual for a given scene state (research.md
/// Decisions 5/9). The image itself is never authoritative — it is an interpretation of the Game aggregate.</summary>
public sealed class VisualAsset
{
    public string SceneStateKey { get; }
    public string? BlobUri { get; private set; }
    public VisualAssetStatus Status { get; private set; }
    public DateTimeOffset? GeneratedAt { get; private set; }

    public VisualAsset(string sceneStateKey)
    {
        SceneStateKey = sceneStateKey;
        Status = VisualAssetStatus.Pending;
    }

    public void MarkReady(string blobUri, DateTimeOffset generatedAt)
    {
        BlobUri = blobUri;
        Status = VisualAssetStatus.Ready;
        GeneratedAt = generatedAt;
    }

    public void MarkFailed() => Status = VisualAssetStatus.Failed;
}
