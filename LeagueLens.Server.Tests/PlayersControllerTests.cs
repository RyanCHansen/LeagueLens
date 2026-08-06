using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LeagueLens.Application.Sleeper.Read.Models;
using LeagueLens.Domain.Entities;
using LeagueLens.Domain.Enums;
using LeagueLens.Server.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace LeagueLens.Server.Tests;

public sealed class PlayersControllerTests : IAsyncLifetime
{
    private readonly SleeperTestWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    // See LeaguesControllerTestBase for why this can't just be JsonSerializerOptions.Default:
    // HttpClient.GetFromJsonAsync doesn't inherit the server's JsonStringEnumConverter, and a
    // bare `new JsonSerializerOptions()` isn't case-insensitive like JsonSerializerDefaults.Web,
    // which silently binds every property to its default instead of erroring.
    private static readonly JsonSerializerOptions JsonOptionsWithEnumConverter = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

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

    private async Task SeedPlayerAsync(string sleeperPlayerId, string firstName, string lastName, PlayerPosition position, string? nflTeam) =>
        await _factory.SeedAsync(db =>
        {
            db.Players.Add(new Player
            {
                Id = Guid.NewGuid(),
                SleeperPlayerId = sleeperPlayerId,
                FirstName = firstName,
                LastName = lastName,
                Position = position,
                NflTeam = nflTeam,
            });
            return Task.CompletedTask;
        });

    [Fact]
    public async Task GetPlayer_ReturnsPlayerDetail_WhenSynced()
    {
        await SeedPlayerAsync("p1", "Josh", "Allen", PlayerPosition.QB, "BUF");

        var result = await _client.GetFromJsonAsync<PlayerDetailResult>("/api/players/p1", JsonOptionsWithEnumConverter);

        Assert.NotNull(result);
        Assert.Equal("p1", result.SleeperPlayerId);
        Assert.Equal("Josh", result.FirstName);
        Assert.Equal("Allen", result.LastName);
        Assert.Equal(PlayerPosition.QB, result.Position);
        Assert.Equal("BUF", result.NflTeam);
    }

    [Fact]
    public async Task GetPlayer_ReturnsProblemDetails404_WhenPlayerUnknown()
    {
        var response = await _client.GetAsync("/api/players/unknown");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Player not found", problem.Title);
    }

    [Fact]
    public async Task SearchPlayers_MatchesFullNameCaseInsensitively()
    {
        await SeedPlayerAsync("p1", "Josh", "Allen", PlayerPosition.QB, "BUF");
        await SeedPlayerAsync("p2", "Patrick", "Mahomes", PlayerPosition.QB, "KC");

        var result = await _client.GetFromJsonAsync<PlayerSearchResult>("/api/players?search=josh all", JsonOptionsWithEnumConverter);

        Assert.NotNull(result);
        var player = Assert.Single(result.Players);
        Assert.Equal("p1", player.SleeperPlayerId);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task SearchPlayers_FiltersByExactPosition()
    {
        await SeedPlayerAsync("p1", "Josh", "Allen", PlayerPosition.QB, "BUF");
        await SeedPlayerAsync("p2", "Christian", "McCaffrey", PlayerPosition.RB, "SF");

        var result = await _client.GetFromJsonAsync<PlayerSearchResult>("/api/players?position=RB", JsonOptionsWithEnumConverter);

        Assert.NotNull(result);
        var player = Assert.Single(result.Players);
        Assert.Equal("p2", player.SleeperPlayerId);
    }

    [Fact]
    public async Task SearchPlayers_PaginatesResults_AndReportsFullTotalCount()
    {
        for (var i = 1; i <= 5; i++)
            await SeedPlayerAsync($"p{i}", "First", $"Last{i}", PlayerPosition.WR, "NYJ");

        var result = await _client.GetFromJsonAsync<PlayerSearchResult>("/api/players?page=2&pageSize=2", JsonOptionsWithEnumConverter);

        Assert.NotNull(result);
        Assert.Equal(2, result.Players.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        // Sorted by LastName then FirstName -- page 2 of size 2 is items 3-4 (Last3, Last4).
        Assert.Equal(["Last3", "Last4"], result.Players.Select(p => p.LastName).ToArray());
    }

    [Fact]
    public async Task SearchPlayers_ReturnsEmptyResults_WhenNoPlayersSyncedYet()
    {
        var result = await _client.GetFromJsonAsync<PlayerSearchResult>("/api/players", JsonOptionsWithEnumConverter);

        Assert.NotNull(result);
        Assert.Empty(result.Players);
        Assert.Equal(0, result.TotalCount);
    }
}
