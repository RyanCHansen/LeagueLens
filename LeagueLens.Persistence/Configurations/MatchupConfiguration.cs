using LeagueLens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeagueLens.Persistence.Configurations;

public class MatchupConfiguration : IEntityTypeConfiguration<Matchup>
{
    public void Configure(EntityTypeBuilder<Matchup> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.HomeScore).HasPrecision(6, 2);
        builder.Property(m => m.AwayScore).HasPrecision(6, 2);

        builder.HasIndex(m => new { m.LeagueId, m.Week });

        builder.HasOne<League>()
            .WithMany()
            .HasForeignKey(m => m.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict on both membership FKs: League already cascades into
        // LeagueMembership, so cascading these too would create multiple
        // cascade paths from League down to Matchup (SQL Server rejects that).
        builder.HasOne<LeagueMembership>()
            .WithMany()
            .HasForeignKey(m => m.HomeLeagueMembershipId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LeagueMembership>()
            .WithMany()
            .HasForeignKey(m => m.AwayLeagueMembershipId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
