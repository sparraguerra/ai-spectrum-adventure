namespace AI.SpectrumAdventure.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

public sealed class AdventureDbContext(DbContextOptions<AdventureDbContext> options) : DbContext(options)
{
    public DbSet<GameRecord> Games => Set<GameRecord>();

    public DbSet<AdventureRecord> Adventures => Set<AdventureRecord>();

    public DbSet<VisualAssetRecord> VisualAssets => Set<VisualAssetRecord>();

    public DbSet<WorldRecord> Worlds => Set<WorldRecord>();
    public DbSet<RegionRecord> WorldRegions => Set<RegionRecord>();
    public DbSet<WorldLocationRecord> WorldLocations => Set<WorldLocationRecord>();
    public DbSet<WorldPresentationRecord> WorldPresentations => Set<WorldPresentationRecord>();
    public DbSet<WorldConnectionRecord> WorldConnections => Set<WorldConnectionRecord>();
    public DbSet<GenerationMetadataRecord> WorldGenerationMetadata => Set<GenerationMetadataRecord>();
    public DbSet<WorldEventRecord> WorldEvents => Set<WorldEventRecord>();
    public DbSet<LoreRecord> WorldLore => Set<LoreRecord>();
    public DbSet<WorldNpcStateRecord> WorldNpcStates => Set<WorldNpcStateRecord>();
    public DbSet<WorldPuzzleRecord> WorldPuzzles => Set<WorldPuzzleRecord>();
    public DbSet<AdventureDraftRecord> AdventureDrafts => Set<AdventureDraftRecord>();
    public DbSet<AdventureVersionRecord> AdventureVersions => Set<AdventureVersionRecord>();
    public DbSet<AuthoringProposalRecord> AuthoringProposals => Set<AuthoringProposalRecord>();
    public DbSet<AuthoringAuditEntryRecord> AuthoringAuditEntries => Set<AuthoringAuditEntryRecord>();
    public DbSet<PlaytestSessionRecord> PlaytestSessions => Set<PlaytestSessionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdventureDbContext).Assembly);
    }
}
