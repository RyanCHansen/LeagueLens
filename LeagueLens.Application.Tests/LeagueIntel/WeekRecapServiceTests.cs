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
        Assert.Equal([100, 90], Both(entry).Select(p => p.Score));
    }

    [Fact]
    public async Task GetWeekRecapAsync_AssignsWinAndLoss_PerParticipant()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        AddMatchup(week: 1, alpha.Id, 100, beta.Id, 90);

        var lookup = await GetRecapAsync(week: 1);

        var entry = Assert.Single(lookup.Recap!.Matchups);
        Assert.Equal(10, entry.MarginOfVictory);
        var byMembership = Both(entry).ToDictionary(p => p.LeagueMembershipId);
        Assert.Equal("Alpha", byMembership[alpha.Id].TeamName);
        Assert.Equal(100, byMembership[alpha.Id].Score);
        Assert.Equal(MatchupOutcome.Win, byMembership[alpha.Id].Outcome);
        Assert.Equal("Beta", byMembership[beta.Id].TeamName);
        Assert.Equal(90, byMembership[beta.Id].Score);
        Assert.Equal(MatchupOutcome.Loss, byMembership[beta.Id].Outcome);
    }

    [Fact]
    public async Task GetWeekRecapAsync_AssignsWinAndLoss_RegardlessOfWhichParticipantScoredHigher()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        AddMatchup(week: 1, alpha.Id, 80, beta.Id, 95);

        var lookup = await GetRecapAsync(week: 1);

        var entry = Assert.Single(lookup.Recap!.Matchups);
        Assert.Equal(15, entry.MarginOfVictory);
        var byMembership = Both(entry).ToDictionary(p => p.LeagueMembershipId);
        Assert.Equal(MatchupOutcome.Loss, byMembership[alpha.Id].Outcome);
        Assert.Equal(MatchupOutcome.Win, byMembership[beta.Id].Outcome);
    }

    [Fact]
    public async Task GetWeekRecapAsync_AssignsTie_ToBothParticipants_WhenScoresAreEqual()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        AddMatchup(week: 1, alpha.Id, 100, beta.Id, 100);

        var lookup = await GetRecapAsync(week: 1);

        var entry = Assert.Single(lookup.Recap!.Matchups);
        Assert.Equal(0, entry.MarginOfVictory);
        Assert.All(Both(entry), p => Assert.Equal(MatchupOutcome.Tie, p.Outcome));
    }

    [Fact]
    public async Task GetWeekRecapAsync_AssignsTeamASlot_ByTeamName()
    {
        var beta = AddMembership("Beta", "u1");
        var alpha = AddMembership("Alpha", "u2");
        AddMatchup(week: 1, beta.Id, 90, alpha.Id, 100);

        var lookup = await GetRecapAsync(week: 1);

        var entry = Assert.Single(lookup.Recap!.Matchups);
        Assert.Equal("Alpha", entry.TeamA.TeamName);
        Assert.Equal("Beta", entry.TeamB.TeamName);
    }

    [Fact]
    public async Task GetWeekRecapAsync_OrdersMatchupsDeterministically_ByTeamASlotTeamName()
    {
        var zeta = AddMembership("Zeta", "u1");
        var zetaOpponent = AddMembership("ZetaOpponent", "u2");
        var alpha = AddMembership("Alpha", "u3");
        var alphaOpponent = AddMembership("AlphaOpponent", "u4");
        AddMatchup(week: 1, zeta.Id, 100, zetaOpponent.Id, 90);
        AddMatchup(week: 1, alpha.Id, 100, alphaOpponent.Id, 90);

        var lookup = await GetRecapAsync(week: 1);

        Assert.Equal(["Alpha", "Zeta"], lookup.Recap!.Matchups.Select(m => m.TeamA.TeamName));
    }

    private static IEnumerable<MatchupParticipantResult> Both(MatchupRecapEntry entry) => [entry.TeamA, entry.TeamB];
}
