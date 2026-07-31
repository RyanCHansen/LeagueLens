using LeagueLens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeagueLens.Persistence.Configurations;

public class RosterConfiguration : IEntityTypeConfiguration<Roster>
{
    public void Configure(EntityTypeBuilder<Roster> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Slot)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(r => new { r.LeagueMembershipId, r.Season });

        builder.HasOne<LeagueMembership>()
            .WithMany()
            .HasForeignKey(r => r.LeagueMembershipId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, not Cascade: Player is a shared, global entity (ADR-003) —
        // deleting one shouldn't silently wipe league roster history.
        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(r => r.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
