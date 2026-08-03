using LeagueLens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeagueLens.Persistence.Configurations;

public class MatchupConfiguration : IEntityTypeConfiguration<Matchup>
{
    public void Configure(EntityTypeBuilder<Matchup> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasIndex(m => new { m.LeagueId, m.Week });

        builder.HasOne<League>()
            .WithMany()
            .HasForeignKey(m => m.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
