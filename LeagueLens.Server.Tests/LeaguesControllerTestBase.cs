using System.Text.Json;
using System.Text.Json.Serialization;
using LeagueLens.Domain.Entities;
using LeagueLens.Server.Tests.TestSupport;

namespace LeagueLens.Server.Tests;

/// <summary>Shared factory/client lifecycle and league-seeding for LeaguesController tests, split by endpoint area (preview/sync/read).</summary>
public abstract class LeaguesControllerTestBase : IAsyncLifetime
{
    protected readonly SleeperTestWebApplicationFactory _factory = new();
    protected HttpClient _client = null!;

    // HttpClient.GetFromJsonAsync uses JsonSerializerOptions.Default unless told otherwise, which
    // doesn't know about Program.cs's JsonStringEnumConverter -- responses containing an enum
    // field (e.g. RosterPlayerEntry.Position/Slot) need this passed explicitly to deserialize.
    // Based on JsonSerializerDefaults.Web (camelCase, case-insensitive) to match what
    // AddControllers()'s JSON formatter actually uses server-side -- a bare `new
    // JsonSerializerOptions()` doesn't have those defaults and silently binds every property to
    // its type's default value instead of erroring, which is worse to debug than a clear failure.
    protected static readonly JsonSerializerOptions JsonOptionsWithEnumConverter = new(JsonSerializerDefaults.Web)
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

    protected async Task<(Guid LeagueId, Guid Membership1Id, Guid Membership2Id)> SeedLeagueAsync(
        string sleeperLeagueId, int season = 2026)
    {
        var leagueId = Guid.NewGuid();
        var membership1Id = Guid.NewGuid();
        var membership2Id = Guid.NewGuid();

        await _factory.SeedAsync(db =>
        {
            db.Leagues.Add(new League { Id = leagueId, SleeperLeagueId = sleeperLeagueId, Name = "Test League", Season = season });
            db.LeagueMemberships.Add(new LeagueMembership { Id = membership1Id, LeagueId = leagueId, SleeperUserId = "u1", TeamName = "Alpha" });
            db.LeagueMemberships.Add(new LeagueMembership { Id = membership2Id, LeagueId = leagueId, SleeperUserId = "u2", TeamName = "Beta" });
            return Task.CompletedTask;
        });

        return (leagueId, membership1Id, membership2Id);
    }
}
