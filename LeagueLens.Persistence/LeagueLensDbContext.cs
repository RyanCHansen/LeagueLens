using LeagueLens.Domain.Entities;
using LeagueLens.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LeagueLens.Persistence;

public class LeagueLensDbContext(DbContextOptions<LeagueLensDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<LeagueMembership> LeagueMemberships => Set<LeagueMembership>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Roster> Rosters => Set<Roster>();
    public DbSet<Matchup> Matchups => Set<Matchup>();
    public DbSet<MatchupParticipant> MatchupParticipants => Set<MatchupParticipant>();
    public DbSet<SyncStatus> SyncStatuses => Set<SyncStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeagueLensDbContext).Assembly);
    }
}
