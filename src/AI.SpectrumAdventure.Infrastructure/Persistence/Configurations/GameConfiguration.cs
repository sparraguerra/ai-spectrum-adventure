namespace AI.SpectrumAdventure.Infrastructure.Persistence.Configurations;

using AI.SpectrumAdventure.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class GameConfiguration : IEntityTypeConfiguration<GameRecord>
{
    public void Configure(EntityTypeBuilder<GameRecord> builder)
    {
        builder.ToTable("games");
        builder.HasKey(g => g.Id);
        builder.HasIndex(g => g.AdventureVersionId);
        builder.Property(g => g.Json).HasColumnType("jsonb").IsRequired();
        builder.Property(g => g.ConcurrencyToken).IsConcurrencyToken();
    }
}
