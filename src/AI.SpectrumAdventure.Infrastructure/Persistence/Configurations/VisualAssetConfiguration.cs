namespace AI.SpectrumAdventure.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

public sealed class VisualAssetRecord
{
    public string SceneStateKey { get; set; } = string.Empty;

    public string? BlobUri { get; set; }

    public required string Status { get; set; }

    public DateTimeOffset? GeneratedAt { get; set; }
}

public sealed class VisualAssetConfiguration : IEntityTypeConfiguration<VisualAssetRecord>
{
    public void Configure(EntityTypeBuilder<VisualAssetRecord> builder)
    {
        builder.ToTable("visual_assets");
        builder.HasKey(a => a.SceneStateKey);
    }
}
