using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Sync;
using LeagueLens.Application.Tests.TestSupport;
using LeagueLens.Domain.Entities;
using LeagueLens.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeagueLens.Application.Tests.Sleeper.Sync;

public class SleeperSyncServiceTests : SqliteBackedTestBase
{
    private static readonly List<string> RosterPositions =
        ["QB", "RB", "WR", "BN", "BN"];

    private readonly FakeSleeperApiClient _client = new();
    private readonly SleeperSyncService _service;

    public SleeperSyncServiceTests()
    {
        _service = new SleeperSyncService(
            _client,
            Db,
            new LeagueSyncer(Db),
            new LeagueMembershipSyncer(Db),
            new RosterSyncer(Db),
            new MatchupSyncer(Db),
            new PlayerCatalogSyncer(Db, NullLogger<PlayerCatalogSyncer>.Instance),
            NullLogger<SleeperSyncService>.Instance);

        _client.League = new SleeperLeagueDto { LeagueId = "L1", Name = "Test League", Season = "2026", RosterPositions = RosterPositions };
        _client.NflState = new SleeperNflStateDto { Season = "2026", Week = 3 }; // weeks 1-2 are "complete"
        _client.Users =
        [
            new SleeperUserDto { UserId = "u1", Metadata = new SleeperUserMetadataDto { TeamName = "Team One" } },
            new SleeperUserDto { UserId = "u2", Metadata = new SleeperUserMetadataDto { TeamName = "Team Two" } },
        ];
        _client.Rosters =
        [
            new SleeperRosterDto { RosterId = 1, OwnerId = "u1", Starters = ["p_qb1", "p_rb1", "p_wr1"], Players = ["p_qb1", "p_rb1", "p_wr1"] },
            new SleeperRosterDto { RosterId = 2, OwnerId = "u2", Starters = ["p_qb2", "p_rb2", "p_wr2"], Players = ["p_qb2", "p_rb2", "p_wr2"] },
        ];
        _client.MatchupsByWeek = new Dictionary<int, List<SleeperMatchupDto>>
        {
            [1] = [new() { RosterId = 1, MatchupId = 1, Points = 100 }, new() { RosterId = 2, MatchupId = 1, Points = 90 }],
            [2] = [new() { RosterId = 1, MatchupId = 1, Points = 105 }, new() { RosterId = 2, MatchupId = 1, Points = 95 }],
        };

        SeedPlayer("p_qb1", PlayerPosition.QB);
        SeedPlayer("p_rb1", PlayerPosition.RB);
        SeedPlayer("p_wr1", PlayerPosition.WR);
        SeedPlayer("p_qb2", PlayerPosition.QB);
        SeedPlayer("p_rb2", PlayerPosition.RB);
        SeedPlayer("p_wr2", PlayerPosition.WR);
        Db.SaveChanges();
    }

    private void SeedPlayer(string sleeperPlayerId, PlayerPosition position) =>
        Db.Players.Add(new Player
        {
            Id = Guid.NewGuid(),
            SleeperPlayerId = sleeperPlayerId,
            FirstName = "First",
            LastName = sleeperPlayerId,
            Position = position,
        });

    [Fact]
    public async Task SyncLeagueAsync_CreatesLeagueMembershipsRostersAndMatchups()
    {
        var result = await _service.SyncLeagueAsync("L1", CancellationToken.None);

        Assert.Equal(2026, result.Season);
        Assert.Equal(2, result.ThroughWeek);
        Assert.Equal(1, result.League.Added);
        Assert.Equal(2, result.Memberships.Added);
        Assert.Equal(6, result.Rosters.Added); // 3 players x 2 rosters
        Assert.Equal(2, result.Matchups.Added); // one matchup per week x 2 weeks
        Assert.Empty(result.Warnings);

        Assert.Equal(1, await Db.Leagues.CountAsync());
        Assert.Equal(2, await Db.LeagueMemberships.CountAsync());
        Assert.Equal(6, await Db.Rosters.CountAsync());
        Assert.Equal(2, await Db.Matchups.CountAsync());
    }

    [Fact]
    public async Task SyncLeagueAsync_IsIdempotentAcrossRepeatedRuns()
    {
        await _service.SyncLeagueAsync("L1", CancellationToken.None);
        var second = await _service.SyncLeagueAsync("L1", CancellationToken.None);

        Assert.Equal(0, second.League.Added);
        Assert.Equal(1, second.League.Updated);
        Assert.Equal(0, second.Memberships.Added);
        Assert.Equal(2, second.Memberships.Updated);

        // Roster/Matchup rows are replaced wholesale each sync, not upserted field-by-field,
        // so "Added" stays the same on re-sync but the row count in the DB must not double.
        Assert.Equal(6, second.Rosters.Added);
        Assert.Equal(2, second.Matchups.Added);
        Assert.Equal(1, await Db.Leagues.CountAsync());
        Assert.Equal(2, await Db.LeagueMemberships.CountAsync());
        Assert.Equal(6, await Db.Rosters.CountAsync());
        Assert.Equal(2, await Db.Matchups.CountAsync());
    }

    [Fact]
    public async Task SyncPlayerCatalogAsync_AddsMappablePlayersAndSkipsUnmapped()
    {
        _client.Players = new Dictionary<string, SleeperPlayerDto>
        {
            ["p1"] = new() { FirstName = "New", LastName = "Player", Position = "QB" },
            ["p2"] = new() { FirstName = "IDP", LastName = "Player", Position = "LB" },
        };

        var result = await _service.SyncPlayerCatalogAsync(CancellationToken.None);

        Assert.Equal(1, result.Players.Added);
        Assert.Equal(1, result.Players.Skipped);
        Assert.Equal(7, await Db.Players.CountAsync()); // 6 seeded + 1 newly added
    }
}
