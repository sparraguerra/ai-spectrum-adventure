namespace AI.SpectrumAdventure.Infrastructure.Persistence.Configurations;

using AI.SpectrumAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class AdventureConfiguration : IEntityTypeConfiguration<AdventureRecord>
{
    public void Configure(EntityTypeBuilder<AdventureRecord> builder)
    {
        builder.ToTable("adventures");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).IsRequired();
        builder.Property(a => a.Json).HasColumnType("jsonb").IsRequired();
    }
}