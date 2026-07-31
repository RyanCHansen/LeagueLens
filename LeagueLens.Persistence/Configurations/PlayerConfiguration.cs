using LeagueLens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeagueLens.Persistence.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.SleeperPlayerId)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.SleeperPlayerId).IsUnique();

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.NflTeam)
            .HasMaxLength(10);

        builder.Property(p => p.Position)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);
    }
}
