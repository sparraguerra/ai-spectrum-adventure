namespace AI.SpectrumAdventure.Infrastructure.Persistence.Configurations;

using AI.SpectrumAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class WorldConfiguration : IEntityTypeConfiguration<WorldRecord>
{
    public void Configure(EntityTypeBuilder<WorldRecord> builder)
    {
        builder.ToTable("worlds");
        builder.HasKey(world => world.Id);
        builder.Property(world => world.Seed).IsRequired();
        builder.Property(world => world.GenerationVersion).IsRequired();
        builder.Property(world => world.ConcurrencyToken).IsConcurrencyToken();
        builder.HasIndex(world => world.LegacyGameId).IsUnique();
        builder.HasMany(world => world.Regions).WithOne().HasForeignKey(region => region.WorldId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(world => world.Locations).WithOne().HasForeignKey(location => location.WorldId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(world => world.Connections).WithOne().HasForeignKey(connection => connection.WorldId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(world => world.GenerationMetadata).WithOne().HasForeignKey(metadata => metadata.WorldId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(world => world.Events).WithOne().HasForeignKey(@event => @event.WorldId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WorldChildConfiguration : IEntityTypeConfiguration<RegionRecord>, IEntityTypeConfiguration<WorldLocationRecord>, IEntityTypeConfiguration<WorldPresentationRecord>, IEntityTypeConfiguration<WorldConnectionRecord>, IEntityTypeConfiguration<GenerationMetadataRecord>, IEntityTypeConfiguration<WorldEventRecord>, IEntityTypeConfiguration<LoreRecord>, IEntityTypeConfiguration<WorldNpcStateRecord>, IEntityTypeConfiguration<WorldPuzzleRecord>
{
    public void Configure(EntityTypeBuilder<RegionRecord> builder) { builder.ToTable("world_regions"); builder.HasKey(region => new { region.WorldId, region.Id }); builder.Property(region => region.TerrainProfileJson).HasColumnType("jsonb"); builder.Property(region => region.AllowedDirectionsJson).HasColumnType("jsonb"); }
    public void Configure(EntityTypeBuilder<WorldLocationRecord> builder) { builder.ToTable("world_locations"); builder.HasKey(location => new { location.WorldId, location.Id }); builder.Property(location => location.EnvironmentJson).HasColumnType("jsonb"); }
    public void Configure(EntityTypeBuilder<WorldPresentationRecord> builder) { builder.ToTable("world_presentations"); builder.HasKey(value => new { value.WorldId, value.LocationId, value.SceneVersion }); builder.Property(value => value.VisualCharacteristicsJson).HasColumnType("jsonb"); }
    public void Configure(EntityTypeBuilder<WorldConnectionRecord> builder) { builder.ToTable("world_connections"); builder.HasKey(connection => new { connection.WorldId, connection.Id }); builder.HasIndex(connection => new { connection.WorldId, connection.SourceLocationId, connection.Direction }).IsUnique(); }
    public void Configure(EntityTypeBuilder<GenerationMetadataRecord> builder) { builder.ToTable("world_generation_metadata"); builder.HasKey(metadata => metadata.Id); builder.HasIndex(metadata => new { metadata.WorldId, metadata.GenerationKey }).IsUnique(); }
    public void Configure(EntityTypeBuilder<WorldEventRecord> builder) { builder.ToTable("world_events"); builder.HasKey(@event => @event.Id); builder.HasIndex(@event => new { @event.WorldId, @event.Sequence }).IsUnique(); builder.Property(@event => @event.PayloadJson).HasColumnType("jsonb"); }
    public void Configure(EntityTypeBuilder<LoreRecord> builder) { builder.ToTable("world_lore"); builder.HasKey(record => record.Id); builder.Property(record => record.Json).HasColumnType("jsonb"); builder.HasOne<WorldRecord>().WithMany(record => record.LoreEntries).HasForeignKey(record => record.WorldId); }
    public void Configure(EntityTypeBuilder<WorldNpcStateRecord> builder) { builder.ToTable("world_npc_states"); builder.HasKey(record => record.Id); builder.Property(record => record.Json).HasColumnType("jsonb"); }
    public void Configure(EntityTypeBuilder<WorldPuzzleRecord> builder) { builder.ToTable("world_puzzles"); builder.HasKey(record => record.Id); builder.Property(record => record.Json).HasColumnType("jsonb"); }
}