namespace AI.SpectrumAdventure.Infrastructure.Persistence.Configurations;

using AI.SpectrumAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class AdventureDraftConfiguration : IEntityTypeConfiguration<AdventureDraftRecord>
{
    public void Configure(EntityTypeBuilder<AdventureDraftRecord> builder)
    {
        builder.ToTable("adventure_drafts"); builder.HasKey(x => x.Id); builder.HasIndex(x => x.AdventureIdentifier).IsUnique();
        builder.Property(x => x.DefinitionJson).HasColumnType("jsonb").IsRequired(); builder.Property(x => x.ValidationJson).HasColumnType("jsonb"); builder.Property(x => x.ConcurrencyToken).IsConcurrencyToken();
    }
}

public sealed class AdventureVersionConfiguration : IEntityTypeConfiguration<AdventureVersionRecord>
{
    public void Configure(EntityTypeBuilder<AdventureVersionRecord> builder)
    {
        builder.ToTable("adventure_versions"); builder.HasKey(x => x.Id); builder.HasIndex(x => new { x.DraftId, x.Sequence }).IsUnique(); builder.HasIndex(x => new { x.AdventureIdentifier, x.Sequence }); builder.Property(x => x.DefinitionJson).HasColumnType("jsonb").IsRequired();
    }
}

public sealed class AuthoringProposalConfiguration : IEntityTypeConfiguration<AuthoringProposalRecord>
{
    public void Configure(EntityTypeBuilder<AuthoringProposalRecord> builder) { builder.ToTable("authoring_proposals"); builder.HasKey(x => x.Id); builder.Property(x => x.PatchJson).HasColumnType("jsonb").IsRequired(); builder.Property(x => x.ValidationJson).HasColumnType("jsonb"); }
}

public sealed class AuthoringAuditEntryConfiguration : IEntityTypeConfiguration<AuthoringAuditEntryRecord>
{
    public void Configure(EntityTypeBuilder<AuthoringAuditEntryRecord> builder) { builder.ToTable("authoring_audit_entries"); builder.HasKey(x => x.Id); builder.HasIndex(x => new { x.DraftId, x.OccurredAt }); }
}

public sealed class PlaytestSessionConfiguration : IEntityTypeConfiguration<PlaytestSessionRecord>
{
    public void Configure(EntityTypeBuilder<PlaytestSessionRecord> builder) { builder.ToTable("playtest_sessions"); builder.HasKey(x => x.Id); builder.HasIndex(x => x.GameId).IsUnique(); }
}