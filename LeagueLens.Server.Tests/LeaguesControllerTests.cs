using System.Net;
using System.Net.Http.Json;
using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Preview;
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

        await _factory.SeedAsync(db =>
        {
            db.Leagues.Add(new League { Id = leagueId, SleeperLeagueId = sleeperLeagueId, Name = "Test League", Season = 2026 });
            db.LeagueMemberships.Add(new LeagueMembership { Id = Guid.NewGuid(), LeagueId = leagueId, SleeperUserId = "u1", TeamName = "Alpha" });
            db.LeagueMemberships.Add(new LeagueMembership { Id = Guid.NewGuid(), LeagueId = leagueId, SleeperUserId = "u2", TeamName = "Beta" });
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task GetWeekPreview_ReturnsProblemDetails404_WhenLeagueUnknown()
    {
        var response = await _client.GetAsync("/api/leagues/unknown/preview");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("League not found", problem.Title);
    }

    [Fact]
    public async Task GetWeekPreview_ReturnsPreview_UsingLiveSleeperData()
    {
        await SeedLeagueAsync("L10");
        _factory.SleeperApiClient.NflState = new SleeperNflStateDto { Season = "2026", Week = 3 };
        _factory.SleeperApiClient.Rosters =
        [
            new SleeperRosterDto { RosterId = 1, OwnerId = "u1" },
            new SleeperRosterDto { RosterId = 2, OwnerId = "u2" },
        ];
        _factory.SleeperApiClient.MatchupsByWeek = new Dictionary<int, List<SleeperMatchupDto>>
        {
            [3] =
            [
                new SleeperMatchupDto { RosterId = 1, MatchupId = 100, Points = 0 },
                new SleeperMatchupDto { RosterId = 2, MatchupId = 100, Points = 0 },
            ],
        };

        var result = await _client.GetFromJsonAsync<WeekPreviewResult>("/api/leagues/L10/preview");

        Assert.NotNull(result);
        Assert.Equal(3, result.Week);
        var entry = Assert.Single(result.Matchups);
        Assert.Equal("Alpha", entry.TeamA.TeamName);
        Assert.Equal("Beta", entry.TeamB.TeamName);
    }

    [Fact]
    public async Task GetWeekPreview_ReturnsProblemDetails502_WhenSleeperApiUnavailable()
    {
        await SeedLeagueAsync("L11");
        _factory.SleeperApiClient.ThrowOnCall = new HttpRequestException("simulated Sleeper outage");

        var response = await _client.GetAsync("/api/leagues/L11/preview");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Upstream Sleeper API unavailable", problem.Title);
    }
}
