namespace AI.SpectrumAdventure.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

public sealed class AdventureDbContext(DbContextOptions<AdventureDbContext> options) : DbContext(options)
{
    public DbSet<GameRecord> Games => Set<GameRecord>();

    public DbSet<AdventureRecord> Adventures => Set<AdventureRecord>();

    public DbSet<VisualAssetRecord> VisualAssets => Set<VisualAssetRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdventureDbContext).Assembly);
    }
}
