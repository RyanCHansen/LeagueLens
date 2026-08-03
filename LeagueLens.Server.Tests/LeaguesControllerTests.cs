using System.Net;
using System.Net.Http.Json;
using LeagueLens.Application.LeagueIntel;
using LeagueLens.Domain.Entities;
using LeagueLens.Server.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace LeagueLens.Server.Tests;

public sealed class LeaguesControllerTests : IAsyncLifetime
{
    private readonly SleeperTestWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task SeedLeagueAsync(string sleeperLeagueId)
    {
        var leagueId = Guid.NewGuid();
        var alphaId = Guid.NewGuid();
        var betaId = Guid.NewGuid();

        await _factory.SeedAsync(db =>
        {
            db.Leagues.Add(new League { Id = leagueId, SleeperLeagueId = sleeperLeagueId, Name = "Test League", Season = 2026 });
            db.LeagueMemberships.Add(new LeagueMembership { Id = alphaId, LeagueId = leagueId, SleeperUserId = "u1", TeamName = "Alpha" });
            db.LeagueMemberships.Add(new LeagueMembership { Id = betaId, LeagueId = leagueId, SleeperUserId = "u2", TeamName = "Beta" });
            db.Matchups.Add(new Matchup
            {
                Id = Guid.NewGuid(),
                LeagueId = leagueId,
                Week = 1,
                HomeLeagueMembershipId = alphaId,
                HomeScore = 100,
                AwayLeagueMembershipId = betaId,
                AwayScore = 90,
            });
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task GetStandings_ReturnsStandings_WhenLeagueExists()
    {
        await SeedLeagueAsync("L1");

        var result = await _client.GetFromJsonAsync<StandingsResult>("/api/leagues/L1/standings");

        Assert.NotNull(result);
        Assert.Equal("L1", result.SleeperLeagueId);
        Assert.Equal(2026, result.Season);
        Assert.Equal(1, result.ThroughWeek);
        Assert.Equal(2, result.Standings.Count);
        Assert.Equal("Alpha", result.Standings[0].TeamName);
    }

    [Fact]
    public async Task GetPowerRankings_ReturnsPowerRankings_WhenLeagueExists()
    {
        await SeedLeagueAsync("L2");

        var result = await _client.GetFromJsonAsync<PowerRankingsResult>("/api/leagues/L2/power-rankings");

        Assert.NotNull(result);
        Assert.Equal(2, result.PowerRankings.Count);
    }

    [Fact]
    public async Task GetTrends_ReturnsTrends_WhenLeagueExists()
    {
        await SeedLeagueAsync("L3");

        var result = await _client.GetFromJsonAsync<TrendsResult>("/api/leagues/L3/trends");

        Assert.NotNull(result);
        Assert.Equal(2, result.Trends.Count);
        Assert.All(result.Trends, t => Assert.Null(t.StandingMovement)); // only 1 week played
    }

    [Fact]
    public async Task GetSummary_ReturnsAllThreeSections_WhenLeagueExists()
    {
        await SeedLeagueAsync("L4");

        var result = await _client.GetFromJsonAsync<LeagueIntelSummary>("/api/leagues/L4/summary");

        Assert.NotNull(result);
        Assert.Equal(2, result.Standings.Count);
        Assert.Equal(2, result.PowerRankings.Count);
        Assert.Equal(2, result.Trends.Count);
    }

    [Theory]
    [InlineData("/api/leagues/unknown/standings")]
    [InlineData("/api/leagues/unknown/power-rankings")]
    [InlineData("/api/leagues/unknown/trends")]
    [InlineData("/api/leagues/unknown/summary")]
    [InlineData("/api/leagues/unknown/weeks/1/recap")]
    public async Task Endpoints_ReturnProblemDetails404_WhenLeagueUnknown(string requestUri)
    {
        var response = await _client.GetAsync(requestUri);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("League not found", problem.Title);
    }

    [Fact]
    public async Task GetWeekRecap_ReturnsRecap_WhenWeekExists()
    {
        await SeedLeagueAsync("L5");

        var result = await _client.GetFromJsonAsync<WeekRecapResult>("/api/leagues/L5/weeks/1/recap");

        Assert.NotNull(result);
        Assert.Equal("L5", result.SleeperLeagueId);
        Assert.Equal(2026, result.Season);
        Assert.Equal(1, result.Week);
        var entry = Assert.Single(result.Matchups);
        Assert.Equal("Alpha", entry.HomeTeamName);
        Assert.Equal(100, entry.HomeScore);
        Assert.Equal("Beta", entry.AwayTeamName);
        Assert.Equal(90, entry.AwayScore);
        Assert.Equal(10, entry.MarginOfVictory);
        Assert.Equal(MatchupWinner.Home, entry.Winner);
    }

    [Fact]
    public async Task GetWeekRecap_SerializesWinner_AsLowercaseString()
    {
        await SeedLeagueAsync("L7");

        var json = await _client.GetStringAsync("/api/leagues/L7/weeks/1/recap");

        Assert.Contains("\"winner\":\"home\"", json);
    }

    [Fact]
    public async Task GetWeekRecap_ReturnsProblemDetails404_WhenWeekHasNoMatchupData()
    {
        await SeedLeagueAsync("L6");

        var response = await _client.GetAsync("/api/leagues/L6/weeks/2/recap");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Week not found", problem.Title);
    }
}
