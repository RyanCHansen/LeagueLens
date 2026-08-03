using LeagueLens.Application.LeagueIntel;
using LeagueLens.Application.Tests.TestSupport;
using LeagueLens.Domain.Entities;
using Microsoft.Extensions.Options;

namespace LeagueLens.Application.Tests.LeagueIntel;

public class LeagueIntelServiceTests : SqliteBackedTestBase
{
    private const string SleeperLeagueId = "L1";

    private readonly ILeagueIntelService _service;
    private readonly Guid _leagueId = Guid.NewGuid();

    public LeagueIntelServiceTests()
    {
        var options = Options.Create(new LeagueIntelOptions());
        _service = new LeagueIntelService(Db, new BlendedPowerRankingCalculator(options), options, TimeProvider.System);

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

    private async Task<LeagueIntelSummary> GetIntelAsync()
    {
        await Db.SaveChangesAsync();
        var summary = await _service.GetLeagueIntelAsync(SleeperLeagueId, CancellationToken.None);
        Assert.NotNull(summary);
        return summary;
    }

    [Fact]
    public async Task GetLeagueIntelAsync_ReturnsNull_WhenLeagueNotSynced()
    {
        await Db.SaveChangesAsync();

        var summary = await _service.GetLeagueIntelAsync("does-not-exist", CancellationToken.None);

        Assert.Null(summary);
    }

    [Fact]
    public async Task GetLeagueIntelAsync_PopulatesMetadata()
    {
        AddMembership("Alpha", "u1");
        var summary = await GetIntelAsync();

        Assert.Equal(SleeperLeagueId, summary.SleeperLeagueId);
        Assert.Equal(2026, summary.Season);
        Assert.Equal(0, summary.ThroughWeek); // no matchups played
    }

    [Fact]
    public async Task Standings_OrdersByWinPctThenPointsFor()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        AddMatchup(week: 1, alpha.Id, 100, beta.Id, 90);
        AddMatchup(week: 2, alpha.Id, 100, beta.Id, 90);

        var summary = await GetIntelAsync();

        Assert.Equal(2, summary.ThroughWeek);
        var standings = summary.Standings;
        var first = standings[0];
        Assert.Equal(alpha.Id, first.LeagueMembershipId);
        Assert.Equal(2, first.Wins);
        Assert.Equal(0, first.Losses);
        Assert.Equal(200m, first.PointsFor);
        Assert.Equal(1, first.Rank);

        var second = standings[1];
        Assert.Equal(beta.Id, second.LeagueMembershipId);
        Assert.Equal(0, second.Wins);
        Assert.Equal(2, second.Losses);
        Assert.Equal(2, second.Rank);
    }

    [Fact]
    public async Task Standings_TiedScore_CountsAsTieForBothTeams()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        AddMatchup(week: 1, alpha.Id, 100, beta.Id, 100);

        var summary = await GetIntelAsync();

        Assert.All(summary.Standings, s => Assert.Equal(1, s.Ties));
    }

    [Fact]
    public async Task Standings_BreaksTiesDeterministically_ByTeamNameThenMembershipId()
    {
        // No matchups played, so every team is tied at 0-0-0 / 0 points-for; only TeamName
        // (then LeagueMembershipId) should decide order.
        AddMembership("Zeta", "u1");
        AddMembership("Alpha", "u2");
        AddMembership("Charlie", "u3");

        var summary = await GetIntelAsync();

        Assert.Equal(["Alpha", "Charlie", "Zeta"], summary.Standings.Select(s => s.TeamName));
    }

    [Fact]
    public async Task PowerRankings_AllScoreZero_WhenNoGamesPlayed()
    {
        AddMembership("Alpha", "u1");
        AddMembership("Beta", "u2");

        var summary = await GetIntelAsync();

        Assert.Equal(2, summary.PowerRankings.Count);
        Assert.All(summary.PowerRankings, r => Assert.Equal(0m, r.Score));
        Assert.Equal([1, 2], summary.PowerRankings.Select(r => r.Rank));
    }

    [Fact]
    public async Task PowerRankings_OrdersByBlendedScoreDescending()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        // Alpha: 1-0, 100 points (max in league). Beta: 0-1, 50 points.
        AddMatchup(week: 1, alpha.Id, 100, beta.Id, 50);

        var summary = await GetIntelAsync();

        Assert.Equal(alpha.Id, summary.PowerRankings[0].LeagueMembershipId);
        Assert.Equal(1.0m, summary.PowerRankings[0].Score); // winPct 1.0, normalized points-for 1.0
        Assert.Equal(beta.Id, summary.PowerRankings[1].LeagueMembershipId);
        Assert.Equal(0.25m, summary.PowerRankings[1].Score); // winPct 0.0, normalized points-for 0.5
    }

    [Fact]
    public async Task Trends_ReturnsNulls_WhenFewerThanWindowPlusOneWeeksPlayed()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        AddMatchup(week: 1, alpha.Id, 100, beta.Id, 90);

        var summary = await GetIntelAsync();

        Assert.All(summary.Trends, t =>
        {
            Assert.Null(t.StandingMovement);
            Assert.Null(t.ScoringTrend);
        });
    }

    [Fact]
    public async Task Trends_ComputesStandingMovementAndScoringTrend_WhenEnoughWeeksPlayed()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");

        // Alpha wins weeks 1-3 comfortably, Beta wins weeks 4-6 comfortably (and by more),
        // flipping both the standings lead and the recent scoring trend at the week-6 mark.
        AddMatchup(week: 1, alpha.Id, 100, beta.Id, 90);
        AddMatchup(week: 2, alpha.Id, 100, beta.Id, 90);
        AddMatchup(week: 3, alpha.Id, 100, beta.Id, 90);
        AddMatchup(week: 4, alpha.Id, 80, beta.Id, 120);
        AddMatchup(week: 5, alpha.Id, 80, beta.Id, 120);
        AddMatchup(week: 6, alpha.Id, 80, beta.Id, 120);

        var summary = await GetIntelAsync();
        var trends = summary.Trends.ToDictionary(t => t.LeagueMembershipId);

        // Current record is 3-3 for both; Beta leads on points-for (630 vs 540) so Beta is
        // rank 1 now, versus rank 1 for Alpha after week 3 (3-0 vs 0-3).
        Assert.Equal(-1, trends[alpha.Id].StandingMovement);
        Assert.Equal(1, trends[beta.Id].StandingMovement);

        Assert.Equal(-20m, trends[alpha.Id].ScoringTrend); // avg 80 (wks 4-6) - avg 100 (wks 1-3)
        Assert.Equal(30m, trends[beta.Id].ScoringTrend); // avg 120 (wks 4-6) - avg 90 (wks 1-3)
    }
}
