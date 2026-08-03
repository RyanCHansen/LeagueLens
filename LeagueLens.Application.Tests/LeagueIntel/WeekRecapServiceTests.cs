using LeagueLens.Application.LeagueIntel;
using LeagueLens.Application.Tests.TestSupport;
using LeagueLens.Domain.Entities;

namespace LeagueLens.Application.Tests.LeagueIntel;

public class WeekRecapServiceTests : SqliteBackedTestBase
{
    private const string SleeperLeagueId = "L1";

    private readonly IWeekRecapService _service;
    private readonly Guid _leagueId = Guid.NewGuid();

    public WeekRecapServiceTests()
    {
        _service = new WeekRecapService(Db);
        Db.Leagues.Add(new League { Id = _leagueId, SleeperLeagueId = SleeperLeagueId, Name = "Test League", Season = 2026 });
    }

    private LeagueMembership AddMembership(string teamName, string sleeperUserId)
    {
        var membership = new LeagueMembership
        {
            Id = Guid.NewGuid(),
            LeagueId = _leagueId,
            SleeperUserId = sleeperUserId,
            TeamName = teamName,
        };
        Db.LeagueMemberships.Add(membership);
        return membership;
    }

    private void AddMatchup(int week, Guid firstId, decimal firstScore, Guid secondId, decimal secondScore)
    {
        var matchupId = Guid.NewGuid();
        Db.Matchups.Add(new Matchup { Id = matchupId, LeagueId = _leagueId, Week = week });
        Db.MatchupParticipants.Add(new MatchupParticipant { Id = Guid.NewGuid(), MatchupId = matchupId, LeagueMembershipId = firstId, Score = firstScore });
        Db.MatchupParticipants.Add(new MatchupParticipant { Id = Guid.NewGuid(), MatchupId = matchupId, LeagueMembershipId = secondId, Score = secondScore });
    }

    private async Task<WeekRecapLookupResult> GetRecapAsync(int week)
    {
        await Db.SaveChangesAsync();
        return await _service.GetWeekRecapAsync(SleeperLeagueId, week, CancellationToken.None);
    }

    [Fact]
    public async Task GetWeekRecapAsync_ReturnsLeagueDoesNotExist_WhenLeagueNotSynced()
    {
        await Db.SaveChangesAsync();

        var lookup = await _service.GetWeekRecapAsync("does-not-exist", 1, CancellationToken.None);

        Assert.False(lookup.LeagueExists);
        Assert.Null(lookup.Recap);
    }

    [Fact]
    public async Task GetWeekRecapAsync_ReturnsNullRecap_WhenLeagueExistsButWeekHasNoMatchups()
    {
        AddMembership("Alpha", "u1");

        var lookup = await GetRecapAsync(week: 1);

        Assert.True(lookup.LeagueExists);
        Assert.Null(lookup.Recap);
    }

    [Fact]
    public async Task GetWeekRecapAsync_PopulatesMetadata()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        AddMatchup(week: 1, alpha.Id, 100, beta.Id, 90);

        var lookup = await GetRecapAsync(week: 1);

        Assert.NotNull(lookup.Recap);
        Assert.Equal(SleeperLeagueId, lookup.Recap.SleeperLeagueId);
        Assert.Equal(2026, lookup.Recap.Season);
        Assert.Equal(1, lookup.Recap.Week);
    }

    [Fact]
    public async Task GetWeekRecapAsync_ReturnsOnlyMatchupsForRequestedWeek()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        AddMatchup(week: 1, alpha.Id, 100, beta.Id, 90);
        AddMatchup(week: 2, alpha.Id, 80, beta.Id, 70);

        var lookup = await GetRecapAsync(week: 1);

        Assert.NotNull(lookup.Recap);
        var entry = Assert.Single(lookup.Recap.Matchups);
        Assert.Equal(100, entry.HomeScore);
        Assert.Equal(90, entry.AwayScore);
    }

    [Fact]
    public async Task GetWeekRecapAsync_ComputesMarginAndWinner_WhenHomeWins()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        AddMatchup(week: 1, alpha.Id, 100, beta.Id, 90);

        var lookup = await GetRecapAsync(week: 1);

        var entry = Assert.Single(lookup.Recap!.Matchups);
        Assert.Equal(alpha.Id, entry.HomeLeagueMembershipId);
        Assert.Equal("Alpha", entry.HomeTeamName);
        Assert.Equal(beta.Id, entry.AwayLeagueMembershipId);
        Assert.Equal("Beta", entry.AwayTeamName);
        Assert.Equal(10, entry.MarginOfVictory);
        Assert.Equal(MatchupWinner.Home, entry.Winner);
    }

    [Fact]
    public async Task GetWeekRecapAsync_ComputesMarginAndWinner_WhenAwayWins()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        AddMatchup(week: 1, alpha.Id, 80, beta.Id, 95);

        var lookup = await GetRecapAsync(week: 1);

        var entry = Assert.Single(lookup.Recap!.Matchups);
        Assert.Equal(15, entry.MarginOfVictory);
        Assert.Equal(MatchupWinner.Away, entry.Winner);
    }

    [Fact]
    public async Task GetWeekRecapAsync_ComputesTie_WhenScoresAreEqual()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        AddMatchup(week: 1, alpha.Id, 100, beta.Id, 100);

        var lookup = await GetRecapAsync(week: 1);

        var entry = Assert.Single(lookup.Recap!.Matchups);
        Assert.Equal(0, entry.MarginOfVictory);
        Assert.Equal(MatchupWinner.Tie, entry.Winner);
    }

    [Fact]
    public async Task GetWeekRecapAsync_OrdersMatchupsDeterministically_ByHomeTeamName()
    {
        var zeta = AddMembership("Zeta", "u1");
        var alphaOpp = AddMembership("ZetaOpponent", "u2");
        var alpha = AddMembership("Alpha", "u3");
        var alphaOpp2 = AddMembership("AlphaOpponent", "u4");
        AddMatchup(week: 1, zeta.Id, 100, alphaOpp.Id, 90);
        AddMatchup(week: 1, alpha.Id, 100, alphaOpp2.Id, 90);

        var lookup = await GetRecapAsync(week: 1);

        Assert.Equal(["Alpha", "Zeta"], lookup.Recap!.Matchups.Select(m => m.HomeTeamName));
    }
}
