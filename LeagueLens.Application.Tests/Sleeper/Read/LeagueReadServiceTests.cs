using LeagueLens.Application.Sleeper.Read;
using LeagueLens.Application.Tests.TestSupport;
using LeagueLens.Domain.Entities;
using LeagueLens.Domain.Enums;

namespace LeagueLens.Application.Tests.Sleeper.Read;

public class LeagueReadServiceTests : SqliteBackedTestBase
{
    private readonly LeagueReadService _service;

    public LeagueReadServiceTests()
    {
        _service = new LeagueReadService(Db);
    }

    private (Guid LeagueId, Guid Membership1Id, Guid Membership2Id) SeedLeague(string sleeperLeagueId, int season = 2026)
    {
        var leagueId = Guid.NewGuid();
        var membership1Id = Guid.NewGuid();
        var membership2Id = Guid.NewGuid();

        Db.Leagues.Add(new League { Id = leagueId, SleeperLeagueId = sleeperLeagueId, Name = "Test League", Season = season });
        Db.LeagueMemberships.Add(new LeagueMembership { Id = membership1Id, LeagueId = leagueId, SleeperUserId = "u1", TeamName = "Zulu" });
        Db.LeagueMemberships.Add(new LeagueMembership { Id = membership2Id, LeagueId = leagueId, SleeperUserId = "u2", TeamName = "Alpha" });
        Db.SaveChanges();

        return (leagueId, membership1Id, membership2Id);
    }

    private Guid SeedPlayer(string sleeperPlayerId, string lastName, PlayerPosition position)
    {
        var playerId = Guid.NewGuid();
        Db.Players.Add(new Player { Id = playerId, SleeperPlayerId = sleeperPlayerId, FirstName = "First", LastName = lastName, Position = position });
        Db.SaveChanges();
        return playerId;
    }

    [Fact]
    public async Task GetLeagueDetailAsync_ReturnsNull_WhenLeagueUnknown()
    {
        var result = await _service.GetLeagueDetailAsync("unknown", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLeagueDetailAsync_SortsMembershipsByTeamNameOrdinal_NotCreationOrder()
    {
        // Zulu is seeded first, Alpha second -- if the result reflected creation order rather
        // than actually sorting, this test would catch it.
        var (_, membership1Id, membership2Id) = SeedLeague("L1");

        var result = await _service.GetLeagueDetailAsync("L1", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("L1", result.SleeperLeagueId);
        Assert.Equal(2026, result.Season);
        Assert.Equal(["Alpha", "Zulu"], result.Memberships.Select(m => m.TeamName!).ToArray());
        Assert.Equal(membership2Id, result.Memberships[0].LeagueMembershipId); // Alpha
        Assert.Equal(membership1Id, result.Memberships[1].LeagueMembershipId); // Zulu
    }

    [Fact]
    public async Task GetRostersAsync_ReturnsNull_WhenLeagueUnknown()
    {
        var result = await _service.GetRostersAsync("unknown", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRostersAsync_ReturnsEmptyPlayerLists_WhenLeagueSyncedButNoRostersYet()
    {
        SeedLeague("L2");

        var result = await _service.GetRostersAsync("L2", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Empty(t.Players));
    }

    [Fact]
    public async Task GetRostersAsync_EmbedsPlayerIdentity_AndOrdersStartersBeforeBench()
    {
        var (_, membership1Id, _) = SeedLeague("L3");
        var qbId = SeedPlayer("p_qb", "Allen", PlayerPosition.QB);
        var wrId = SeedPlayer("p_wr", "Adams", PlayerPosition.WR);

        // Bench row added first, starter added second -- if the result reflected insertion order
        // rather than actually ordering by RosterSlot, this test would catch it.
        Db.Rosters.Add(new Roster { Id = Guid.NewGuid(), LeagueMembershipId = membership1Id, PlayerId = wrId, Slot = RosterSlot.Bench, Season = 2026 });
        Db.Rosters.Add(new Roster { Id = Guid.NewGuid(), LeagueMembershipId = membership1Id, PlayerId = qbId, Slot = RosterSlot.QB, Season = 2026 });
        Db.SaveChanges();

        var result = await _service.GetRostersAsync("L3", CancellationToken.None);

        Assert.NotNull(result);
        var team = result.Single(t => t.LeagueMembershipId == membership1Id);
        Assert.Equal(2, team.Players.Count);
        Assert.Equal(RosterSlot.QB, team.Players[0].Slot);
        Assert.Equal("Allen", team.Players[0].LastName);
        Assert.Equal(RosterSlot.Bench, team.Players[1].Slot);
        Assert.Equal("Adams", team.Players[1].LastName);
    }

    [Fact]
    public async Task GetRostersAsync_ExcludesRosterRowsFromOtherSeasons()
    {
        var (_, membership1Id, _) = SeedLeague("L4", season: 2026);
        var playerId = SeedPlayer("p1", "Old", PlayerPosition.RB);
        Db.Rosters.Add(new Roster { Id = Guid.NewGuid(), LeagueMembershipId = membership1Id, PlayerId = playerId, Slot = RosterSlot.RB, Season = 2025 });
        Db.SaveChanges();

        var result = await _service.GetRostersAsync("L4", CancellationToken.None);

        Assert.NotNull(result);
        Assert.All(result, t => Assert.Empty(t.Players));
    }

    [Fact]
    public async Task GetMatchupsAsync_ReturnsNull_WhenLeagueUnknown()
    {
        var result = await _service.GetMatchupsAsync("unknown", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMatchupsAsync_ReturnsEmptyList_WhenNoMatchupsYet()
    {
        SeedLeague("L5");

        var result = await _service.GetMatchupsAsync("L5", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMatchupsAsync_OrdersByWeek_AndSortsTeamsByNameOrdinal()
    {
        var (leagueId, zulu, alpha) = SeedLeague("L6"); // membership1=Zulu, membership2=Alpha

        var matchupWeek1 = Guid.NewGuid();
        Db.Matchups.Add(new Matchup { Id = matchupWeek1, LeagueId = leagueId, Week = 1 });
        Db.MatchupParticipants.Add(new MatchupParticipant { Id = Guid.NewGuid(), MatchupId = matchupWeek1, LeagueMembershipId = zulu, Score = 100m });
        Db.MatchupParticipants.Add(new MatchupParticipant { Id = Guid.NewGuid(), MatchupId = matchupWeek1, LeagueMembershipId = alpha, Score = 90m });
        Db.SaveChanges();

        var result = await _service.GetMatchupsAsync("L6", CancellationToken.None);

        Assert.NotNull(result);
        var week1 = Assert.Single(result);
        Assert.Equal(1, week1.Week);
        // "Alpha" < "Zulu" ordinally -- TeamA should be Alpha regardless of which membership was
        // seeded/persisted first.
        Assert.Equal("Alpha", week1.TeamA.TeamName);
        Assert.Equal(90m, week1.TeamA.Score);
        Assert.Equal("Zulu", week1.TeamB.TeamName);
        Assert.Equal(100m, week1.TeamB.Score);
    }
}
