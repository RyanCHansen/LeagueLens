using LeagueLens.Application.LeagueIntel;
using LeagueLens.Application.Tests.TestSupport;
using LeagueLens.Domain.Entities;

namespace LeagueLens.Application.Tests.LeagueIntel;

public class WeekHighlightsServiceTests : SqliteBackedTestBase
{
    private const string SleeperLeagueId = "L1";

    private readonly IWeekHighlightsService _service;
    private readonly Guid _leagueId = Guid.NewGuid();

    public WeekHighlightsServiceTests()
    {
        _service = new WeekHighlightsService(new WeekRecapService(Db));
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

    private async Task<WeekHighlightsLookupResult> GetHighlightsAsync(int week)
    {
        await Db.SaveChangesAsync();
        return await _service.GetWeekHighlightsAsync(SleeperLeagueId, week, CancellationToken.None);
    }

    [Fact]
    public async Task GetWeekHighlightsAsync_ReturnsLeagueDoesNotExist_WhenLeagueNotSynced()
    {
        await Db.SaveChangesAsync();

        var lookup = await _service.GetWeekHighlightsAsync("does-not-exist", 1, CancellationToken.None);

        Assert.False(lookup.LeagueExists);
        Assert.Null(lookup.Highlights);
    }

    [Fact]
    public async Task GetWeekHighlightsAsync_ReturnsNullHighlights_WhenLeagueExistsButWeekHasNoMatchups()
    {
        AddMembership("Alpha", "u1");

        var lookup = await GetHighlightsAsync(week: 1);

        Assert.True(lookup.LeagueExists);
        Assert.Null(lookup.Highlights);
    }

    [Fact]
    public async Task GetWeekHighlightsAsync_PopulatesMetadata()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        AddMatchup(week: 1, alpha.Id, 100, beta.Id, 90);

        var lookup = await GetHighlightsAsync(week: 1);

        Assert.NotNull(lookup.Highlights);
        Assert.Equal(SleeperLeagueId, lookup.Highlights.SleeperLeagueId);
        Assert.Equal(2026, lookup.Highlights.Season);
        Assert.Equal(1, lookup.Highlights.Week);
    }

    [Fact]
    public async Task GetWeekHighlightsAsync_IdentifiesEachHighlight_AcrossMultipleMatchups()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        var charlie = AddMembership("Charlie", "u3");
        var delta = AddMembership("Delta", "u4");
        var echo = AddMembership("Echo", "u5");
        var foxtrot = AddMembership("Foxtrot", "u6");
        AddMatchup(week: 1, alpha.Id, 100, beta.Id, 90); // margin 10
        AddMatchup(week: 1, charlie.Id, 150, delta.Id, 60); // margin 90, highest individual score
        AddMatchup(week: 1, echo.Id, 80, foxtrot.Id, 78); // margin 2, closest

        var lookup = await GetHighlightsAsync(week: 1);
        var highlights = lookup.Highlights!;

        Assert.Equal("Highest Scoring Team", highlights.HighestScoringTeam.Label);
        Assert.Equal(150, Math.Max(highlights.HighestScoringTeam.Matchup.TeamA.Score, highlights.HighestScoringTeam.Matchup.TeamB.Score));

        Assert.Equal("Closest Game", highlights.ClosestGame.Label);
        Assert.Equal(2, highlights.ClosestGame.Matchup.MarginOfVictory);

        Assert.Equal("Biggest Blowout", highlights.BiggestBlowout.Label);
        Assert.Equal(90, highlights.BiggestBlowout.Matchup.MarginOfVictory);
    }

    [Fact]
    public async Task GetWeekHighlightsAsync_BreaksTiesDeterministically_ByTeamASlotTeamName()
    {
        var zeta = AddMembership("Zeta", "u1");
        var zetaOpponent = AddMembership("ZetaOpponent", "u2");
        var alpha = AddMembership("Alpha", "u3");
        var alphaOpponent = AddMembership("AlphaOpponent", "u4");
        AddMatchup(week: 1, zeta.Id, 100, zetaOpponent.Id, 95); // margin 5
        AddMatchup(week: 1, alpha.Id, 100, alphaOpponent.Id, 95); // margin 5, tied

        var lookup = await GetHighlightsAsync(week: 1);

        Assert.Equal("Alpha", lookup.Highlights!.ClosestGame.Matchup.TeamA.TeamName);
        Assert.Equal("Alpha", lookup.Highlights!.BiggestBlowout.Matchup.TeamA.TeamName);
    }

    [Fact]
    public async Task GetWeekHighlightsAsync_ClosestGameAndBiggestBlowout_AreTheSameMatchup_WhenOnlyOneMatchupThatWeek()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        AddMatchup(week: 1, alpha.Id, 100, beta.Id, 90);

        var lookup = await GetHighlightsAsync(week: 1);

        Assert.Equal(lookup.Highlights!.ClosestGame.Matchup, lookup.Highlights.BiggestBlowout.Matchup);
    }
}
