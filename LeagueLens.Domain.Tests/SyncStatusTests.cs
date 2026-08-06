using LeagueLens.Domain.Entities;
using LeagueLens.Domain.Enums;

namespace LeagueLens.Domain.Tests;

public class SyncStatusTests
{
    [Fact]
    public void CanRepresentAGloballyScopedSync()
    {
        var status = new SyncStatus
        {
            Id = Guid.NewGuid(),
            Provider = SyncProvider.Sleeper,
            SyncType = SyncKind.PlayerCatalog,
            SleeperLeagueId = "",
            LastSuccessfulSyncAt = DateTimeOffset.UtcNow,
        };

        Assert.Equal("", status.SleeperLeagueId);
    }

    [Fact]
    public void CanRepresentALeagueScopedSync()
    {
        var status = new SyncStatus
        {
            Id = Guid.NewGuid(),
            Provider = SyncProvider.Sleeper,
            SyncType = SyncKind.League,
            SleeperLeagueId = "sleeper-league-123",
            LastSuccessfulSyncAt = DateTimeOffset.UtcNow,
        };

        Assert.Equal("sleeper-league-123", status.SleeperLeagueId);
    }
}
