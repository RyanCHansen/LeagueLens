using System.Net;
using System.Net.Http.Json;
using LeagueLens.Application.Sleeper.Read.Models;
using LeagueLens.Domain.Entities;
using LeagueLens.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LeagueLens.Server.Tests;

public sealed class LeaguesController_ReadTests : LeaguesControllerTestBase
{
    private async Task<Guid> SeedPlayerAsync(string sleeperPlayerId, string firstName, string lastName, PlayerPosition position, string? nflTeam)
    {
        var playerId = Guid.NewGuid();
        await _factory.SeedAsync(db =>
        {
            db.Players.Add(new Player
            {
                Id = playerId,
                SleeperPlayerId = sleeperPlayerId,
                FirstName = firstName,
                LastName = lastName,
                Position = position,
                NflTeam = nflTeam,
            });
            return Task.CompletedTask;
        });
        return playerId;
    }

    private async Task SeedRosterAsync(Guid leagueMembershipId, Guid playerId, RosterSlot slot, int season) =>
        await _factory.SeedAsync(db =>
        {
            db.Rosters.Add(new Roster { Id = Guid.NewGuid(), LeagueMembershipId = leagueMembershipId, PlayerId = playerId, Slot = slot, Season = season });
            return Task.CompletedTask;
        });

    private async Task SeedMatchupAsync(Guid leagueId, int week, Guid membership1Id, decimal score1, Guid membership2Id, decimal score2) =>
        await _factory.SeedAsync(db =>
        {
            var matchupId = Guid.NewGuid();
            db.Matchups.Add(new Matchup { Id = matchupId, LeagueId = leagueId, Week = week });
            db.MatchupParticipants.Add(new MatchupParticipant { Id = Guid.NewGuid(), MatchupId = matchupId, LeagueMembershipId = membership1Id, Score = score1 });
            db.MatchupParticipants.Add(new MatchupParticipant { Id = Guid.NewGuid(), MatchupId = matchupId, LeagueMembershipId = membership2Id, Score = score2 });
            return Task.CompletedTask;
        });

    [Fact]
    public async Task GetLeague_ReturnsLeagueDetail_WhenSynced()
    {
        var (_, membership1Id, membership2Id) = await SeedLeagueAsync("L30");

        var result = await _client.GetFromJsonAsync<LeagueDetailResult>("/api/leagues/L30");

        Assert.NotNull(result);
        Assert.Equal("L30", result.SleeperLeagueId);
        Assert.Equal(2026, result.Season);
        Assert.Equal(2, result.Memberships.Count);
        Assert.Contains(result.Memberships, m => m.TeamName == "Alpha" && m.LeagueMembershipId == membership1Id);
        Assert.Contains(result.Memberships, m => m.TeamName == "Beta" && m.LeagueMembershipId == membership2Id);
    }

    [Fact]
    public async Task GetLeague_ReturnsProblemDetails404_WhenLeagueUnknown()
    {
        var response = await _client.GetAsync("/api/leagues/unknown");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("League not found", problem.Title);
    }

    [Fact]
    public async Task GetRosters_ReturnsEachTeamsRosterWithEmbeddedPlayerDetails()
    {
        var (_, membership1Id, membership2Id) = await SeedLeagueAsync("L31");
        var playerId = await SeedPlayerAsync("p1", "Josh", "Allen", PlayerPosition.QB, "BUF");
        await SeedRosterAsync(membership1Id, playerId, RosterSlot.QB, season: 2026);

        var result = await _client.GetFromJsonAsync<List<TeamRosterResult>>("/api/leagues/L31/rosters", JsonOptionsWithEnumConverter);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var alpha = result.Single(t => t.LeagueMembershipId == membership1Id);
        var player = Assert.Single(alpha.Players);
        Assert.Equal("p1", player.SleeperPlayerId);
        Assert.Equal("Josh", player.FirstName);
        Assert.Equal("Allen", player.LastName);
        Assert.Equal(PlayerPosition.QB, player.Position);
        Assert.Equal("BUF", player.NflTeam);
        Assert.Equal(RosterSlot.QB, player.Slot);

        var beta = result.Single(t => t.LeagueMembershipId == membership2Id);
        Assert.Empty(beta.Players);
    }

    [Fact]
    public async Task GetRosters_OnlyReturnsCurrentSeasonRosters()
    {
        var (_, membership1Id, _) = await SeedLeagueAsync("L32", season: 2026);
        var playerId = await SeedPlayerAsync("p2", "Old", "Data", PlayerPosition.RB, "SF");
        await SeedRosterAsync(membership1Id, playerId, RosterSlot.RB, season: 2025); // stale season, must not appear

        var result = await _client.GetFromJsonAsync<List<TeamRosterResult>>("/api/leagues/L32/rosters", JsonOptionsWithEnumConverter);

        Assert.NotNull(result);
        Assert.All(result, t => Assert.Empty(t.Players));
    }

    [Fact]
    public async Task GetRosters_ReturnsProblemDetails404_WhenLeagueUnknown()
    {
        var response = await _client.GetAsync("/api/leagues/unknown/rosters");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMatchups_ReturnsAllPersistedMatchupsWithScores()
    {
        var (leagueId, membership1Id, membership2Id) = await SeedLeagueAsync("L33");
        await SeedMatchupAsync(leagueId, week: 1, membership1Id, 100.5m, membership2Id, 90.25m);
        await SeedMatchupAsync(leagueId, week: 2, membership1Id, 88m, membership2Id, 95m);

        var result = await _client.GetFromJsonAsync<List<MatchupResult>>("/api/leagues/L33/matchups");

        Assert.NotNull(result);
        Assert.Equal(new[] { 1, 2 }, result.Select(m => m.Week).ToArray());

        var week1 = result.Single(m => m.Week == 1);
        Assert.Equal("Alpha", week1.TeamA.TeamName); // stable sort: team name ordinal, "Alpha" < "Beta"
        Assert.Equal(100.5m, week1.TeamA.Score);
        Assert.Equal("Beta", week1.TeamB.TeamName);
        Assert.Equal(90.25m, week1.TeamB.Score);
    }

    [Fact]
    public async Task GetMatchups_ReturnsEmptyList_WhenLeagueSyncedButNoMatchupsYet()
    {
        await SeedLeagueAsync("L34");

        var result = await _client.GetFromJsonAsync<List<MatchupResult>>("/api/leagues/L34/matchups");

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMatchups_ReturnsProblemDetails404_WhenLeagueUnknown()
    {
        var response = await _client.GetAsync("/api/leagues/unknown/matchups");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
