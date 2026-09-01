namespace AI.SpectrumAdventure.Infrastructure.Persistence;

using AI.SpectrumAdventure.Application.Abstractions;
using AI.SpectrumAdventure.Domain.Games;
using Microsoft.EntityFrameworkCore;

public sealed class EfVisualAssetRepository(AdventureDbContext dbContext) : IVisualAssetRepository
{
    public async Task<VisualAsset?> FindAsync(string sceneStateKey, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.VisualAssets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.SceneStateKey == sceneStateKey, cancellationToken);

        if (record is null)
        {
            return null;
        }

        var asset = new VisualAsset(record.SceneStateKey);
        if (record.Status == nameof(VisualAssetStatus.Ready) && record.BlobUri is not null && record.GeneratedAt is not null)
        {
            asset.MarkReady(record.BlobUri, record.GeneratedAt.Value);
        }
        else if (record.Status == nameof(VisualAssetStatus.Failed))
        {
            asset.MarkFailed();
        }

        return asset;
    }

    public async Task SaveAsync(VisualAsset asset, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.VisualAssets.FirstOrDefaultAsync(a => a.SceneStateKey == asset.SceneStateKey, cancellationToken);

        if (existing is null)
        {
            dbContext.VisualAssets.Add(new VisualAssetRecord
            {
                SceneStateKey = asset.SceneStateKey,
                BlobUri = asset.BlobUri,
                Status = asset.Status.ToString(),
                GeneratedAt = asset.GeneratedAt,
            });
        }
        else
        {
            existing.BlobUri = asset.BlobUri;
            existing.Status = asset.Status.ToString();
            existing.GeneratedAt = asset.GeneratedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
