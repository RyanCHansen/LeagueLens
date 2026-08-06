using LeagueLens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeagueLens.Persistence.Configurations;

public class SyncStatusConfiguration : IEntityTypeConfiguration<SyncStatus>
{
    public void Configure(EntityTypeBuilder<SyncStatus> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Provider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.SyncType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // Empty string, not null, for globally-scoped sync types (e.g. the player catalog) --
        // keeps this a real unique constraint. SQL Server treats multiple NULLs in a unique
        // index as distinct, which would let duplicate global-scope rows slip in under a race.
        builder.Property(s => s.SleeperLeagueId)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => new { s.Provider, s.SyncType, s.SleeperLeagueId }).IsUnique();
    }
}
