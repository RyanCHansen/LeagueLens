using LeagueLens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeagueLens.Persistence.Configurations;

public class LeagueMembershipConfiguration : IEntityTypeConfiguration<LeagueMembership>
{
    public void Configure(EntityTypeBuilder<LeagueMembership> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.SleeperUserId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.TeamName)
            .HasMaxLength(200);

        builder.HasIndex(m => new { m.LeagueId, m.SleeperUserId }).IsUnique();

        builder.HasOne<League>()
            .WithMany()
            .HasForeignKey(m => m.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(m => m.UserProfileId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
