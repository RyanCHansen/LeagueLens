using System.Net;
using System.Net.Http.Json;
using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Sync.Models;
using LeagueLens.Domain.Entities;
using LeagueLens.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LeagueLens.Server.Tests;

public sealed class LeaguesController_SyncTests : LeaguesControllerTestBase
{
    private async Task SeedSyncStatusAsync(SyncKind syncType, string sleeperLeagueId, DateTimeOffset lastSuccessfulSyncAt) =>
        await _factory.SeedAsync(db =>
        {
            db.SyncStatuses.Add(new SyncStatus
            {
                Id = Guid.NewGuid(),
                Provider = SyncProvider.Sleeper,
                SyncType = syncType,
                SleeperLeagueId = sleeperLeagueId,
                LastSuccessfulSyncAt = lastSuccessfulSyncAt,
            });
            return Task.CompletedTask;
        });

    [Fact]
    public async Task PostSync_SyncsCatalogAndLeague_OnFirstEverSync()
    {
        _factory.SleeperApiClient.NflState = new SleeperNflStateDto { Season = "2026", Week = 1 };
        _factory.SleeperApiClient.League = new SleeperLeagueDto { LeagueId = "L20", Name = "New League", Season = "2026" };

        var response = await _client.PostAsync("/api/leagues/L20/sync", content: null);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LeagueSyncOrchestrationResult>();
        Assert.NotNull(result);
        Assert.True(result.PlayerCatalog.Ran);
        Assert.True(result.League.Ran);
        Assert.Equal(1, result.League.Result?.League.Added);
    }

    [Fact]
    public async Task PostSync_SkipsCatalogWithinCooldown_ButStillSyncsLeague()
    {
        await SeedSyncStatusAsync(SyncKind.PlayerCatalog, "", DateTimeOffset.UtcNow.AddMinutes(-5)); // cooldown is 60 minutes
        _factory.SleeperApiClient.NflState = new SleeperNflStateDto { Season = "2026", Week = 1 };
        _factory.SleeperApiClient.League = new SleeperLeagueDto { LeagueId = "L21", Name = "Another League", Season = "2026" };

        var response = await _client.PostAsync("/api/leagues/L21/sync", content: null);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LeagueSyncOrchestrationResult>();
        Assert.NotNull(result);
        Assert.False(result.PlayerCatalog.Ran);
        Assert.True(result.League.Ran);
    }

    [Fact]
    public async Task PostSync_SkipsLeagueWithinCooldown()
    {
        await SeedLeagueAsync("L22");
        await SeedSyncStatusAsync(SyncKind.League, "L22", DateTimeOffset.UtcNow.AddMinutes(-2)); // cooldown is 10 minutes

        var response = await _client.PostAsync("/api/leagues/L22/sync", content: null);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LeagueSyncOrchestrationResult>();
        Assert.NotNull(result);
        Assert.False(result.League.Ran);
        Assert.True(result.League.NextSyncAvailableAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task PostSync_ReturnsProblemDetails502_WhenSleeperApiUnavailable()
    {
        _factory.SleeperApiClient.ThrowOnCall = new HttpRequestException("simulated Sleeper outage");

        var response = await _client.PostAsync("/api/leagues/L23/sync", content: null);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Upstream Sleeper API unavailable", problem.Title);
    }

    [Fact]
    public async Task PostSync_ReturnsProblemDetails404_WhenSleeperLeagueDoesNotExist()
    {
        // Skip the catalog step via cooldown so only the league step is exercised -- the fake
        // throws unconditionally for every call once ThrowOnCall is set, so whichever Sleeper
        // call the league step makes first surfaces this exception, same as GetLeagueAsync
        // throwing it for real (see SleeperApiClient.GetLeagueAsync) would.
        await SeedSyncStatusAsync(SyncKind.PlayerCatalog, "", DateTimeOffset.UtcNow);
        _factory.SleeperApiClient.ThrowOnCall = new InvalidOperationException("Sleeper league 'unknown' was not found.");

        var response = await _client.PostAsync("/api/leagues/unknown/sync", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Sleeper league not found", problem.Title);
    }
}
