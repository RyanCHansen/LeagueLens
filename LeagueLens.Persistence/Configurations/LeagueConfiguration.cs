using LeagueLens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeagueLens.Persistence.Configurations;

public class LeagueConfiguration : IEntityTypeConfiguration<League>
{
    public void Configure(EntityTypeBuilder<League> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.SleeperLeagueId)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(l => l.SleeperLeagueId).IsUnique();

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);
    }
}
