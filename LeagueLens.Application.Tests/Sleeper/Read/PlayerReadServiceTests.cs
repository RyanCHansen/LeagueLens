using LeagueLens.Application.Sleeper.Read;
using LeagueLens.Application.Tests.TestSupport;
using LeagueLens.Domain.Entities;
using LeagueLens.Domain.Enums;

namespace LeagueLens.Application.Tests.Sleeper.Read;

public class PlayerReadServiceTests : SqliteBackedTestBase
{
    private readonly PlayerReadService _service;

    public PlayerReadServiceTests()
    {
        _service = new PlayerReadService(Db);
    }

    private void SeedPlayer(string sleeperPlayerId, string firstName, string lastName, PlayerPosition position, string? nflTeam = null)
    {
        Db.Players.Add(new Player
        {
            Id = Guid.NewGuid(),
            SleeperPlayerId = sleeperPlayerId,
            FirstName = firstName,
            LastName = lastName,
            Position = position,
            NflTeam = nflTeam,
        });
        Db.SaveChanges();
    }

    [Fact]
    public async Task GetPlayerAsync_ReturnsNull_WhenPlayerUnknown()
    {
        var result = await _service.GetPlayerAsync("unknown", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPlayerAsync_ReturnsPlayerDetail_WhenSynced()
    {
        SeedPlayer("p1", "Josh", "Allen", PlayerPosition.QB, "BUF");

        var result = await _service.GetPlayerAsync("p1", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Josh", result.FirstName);
        Assert.Equal(PlayerPosition.QB, result.Position);
        Assert.Equal("BUF", result.NflTeam);
    }

    [Fact]
    public async Task SearchPlayersAsync_MatchesSubstringAcrossFirstAndLastName_CaseInsensitively()
    {
        SeedPlayer("p1", "Josh", "Allen", PlayerPosition.QB);
        SeedPlayer("p2", "Patrick", "Mahomes", PlayerPosition.QB);

        // Spans the space between first and last name -- only matches because search is against
        // the concatenated "First Last", not each field independently.
        var result = await _service.SearchPlayersAsync("osh all", null, page: 1, pageSize: 25, CancellationToken.None);

        var player = Assert.Single(result.Players);
        Assert.Equal("p1", player.SleeperPlayerId);
    }

    [Fact]
    public async Task SearchPlayersAsync_FiltersByExactPosition()
    {
        SeedPlayer("p1", "Josh", "Allen", PlayerPosition.QB);
        SeedPlayer("p2", "Christian", "McCaffrey", PlayerPosition.RB);

        var result = await _service.SearchPlayersAsync(null, PlayerPosition.RB, page: 1, pageSize: 25, CancellationToken.None);

        var player = Assert.Single(result.Players);
        Assert.Equal("p2", player.SleeperPlayerId);
    }

    [Fact]
    public async Task SearchPlayersAsync_CombinesSearchAndPositionFilters()
    {
        SeedPlayer("p1", "Josh", "Allen", PlayerPosition.QB);
        SeedPlayer("p2", "Keenan", "Allen", PlayerPosition.WR);

        var result = await _service.SearchPlayersAsync("allen", PlayerPosition.WR, page: 1, pageSize: 25, CancellationToken.None);

        var player = Assert.Single(result.Players);
        Assert.Equal("p2", player.SleeperPlayerId);
    }

    [Fact]
    public async Task SearchPlayersAsync_OrdersByLastNameThenFirstName_AndPaginatesDeterministically()
    {
        SeedPlayer("p1", "Zeke", "Adams", PlayerPosition.RB);
        SeedPlayer("p2", "Amon", "Adams", PlayerPosition.WR);
        SeedPlayer("p3", "First", "Baker", PlayerPosition.QB);

        var page1 = await _service.SearchPlayersAsync(null, null, page: 1, pageSize: 2, CancellationToken.None);
        var page2 = await _service.SearchPlayersAsync(null, null, page: 2, pageSize: 2, CancellationToken.None);

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(["p2", "p1"], page1.Players.Select(p => p.SleeperPlayerId).ToArray()); // Adams: Amon before Zeke
        Assert.Equal(["p3"], page2.Players.Select(p => p.SleeperPlayerId).ToArray());
    }

    [Fact]
    public async Task SearchPlayersAsync_ClampsNonPositivePageAndPageSize()
    {
        SeedPlayer("p1", "Josh", "Allen", PlayerPosition.QB);

        var result = await _service.SearchPlayersAsync(null, null, page: 0, pageSize: -5, CancellationToken.None);

        Assert.Equal(1, result.Page);
        Assert.Equal(25, result.PageSize); // non-positive pageSize falls back to the default, not zero
        Assert.Single(result.Players);
    }

    [Fact]
    public async Task SearchPlayersAsync_ClampsOversizedPageSize()
    {
        var result = await _service.SearchPlayersAsync(null, null, page: 1, pageSize: 500, CancellationToken.None);

        Assert.Equal(100, result.PageSize);
    }
}
