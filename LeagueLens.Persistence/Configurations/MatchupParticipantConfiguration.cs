using LeagueLens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeagueLens.Persistence.Configurations;

public class MatchupParticipantConfiguration : IEntityTypeConfiguration<MatchupParticipant>
{
    public void Configure(EntityTypeBuilder<MatchupParticipant> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Score).HasPrecision(6, 2);

        builder.HasIndex(p => new { p.MatchupId, p.LeagueMembershipId }).IsUnique();

        // Cascade: participants belong to their matchup and have no meaning without it.
        builder.HasOne<Matchup>()
            .WithMany()
            .HasForeignKey(p => p.MatchupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: Matchup already cascades from League, so cascading this too would create
        // a second cascade path from League down to MatchupParticipant (SQL Server rejects that).
        builder.HasOne<LeagueMembership>()
            .WithMany()
            .HasForeignKey(p => p.LeagueMembershipId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
