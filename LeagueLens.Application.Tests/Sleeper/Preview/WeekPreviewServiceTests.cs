using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Preview;
using LeagueLens.Application.Tests.TestSupport;
using LeagueLens.Domain.Entities;

namespace LeagueLens.Application.Tests.Sleeper.Preview;

public class WeekPreviewServiceTests : SqliteBackedTestBase
{
    private const string SleeperLeagueId = "L1";

    private readonly FakeSleeperApiClient _client = new();
    private readonly WeekPreviewService _service;
    private readonly Guid _leagueId = Guid.NewGuid();

    public WeekPreviewServiceTests()
    {
        _service = new WeekPreviewService(Db, _client);
        Db.Leagues.Add(new League { Id = _leagueId, SleeperLeagueId = SleeperLeagueId, Name = "Test League", Season = 2026 });
        _client.NflState = new SleeperNflStateDto { Season = "2026", Week = 5 };
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

    private async Task<WeekPreviewResult?> GetPreviewAsync()
    {
        await Db.SaveChangesAsync();
        return await _service.GetWeekPreviewAsync(SleeperLeagueId, CancellationToken.None);
    }

    [Fact]
    public async Task GetWeekPreviewAsync_ReturnsNull_WhenLeagueNotSynced()
    {
        await Db.SaveChangesAsync();

        var preview = await _service.GetWeekPreviewAsync("does-not-exist", CancellationToken.None);

        Assert.Null(preview);
    }

    [Fact]
    public async Task GetWeekPreviewAsync_UsesCurrentWeek_FromNflState()
    {
        AddMembership("Alpha", "u1");
        _client.Rosters = [];
        _client.MatchupsByWeek = new Dictionary<int, List<SleeperMatchupDto>> { [5] = [] };

        var preview = await GetPreviewAsync();

        Assert.NotNull(preview);
        Assert.Equal(5, preview.Week);
        Assert.Empty(preview.Matchups);
    }

    [Fact]
    public async Task GetWeekPreviewAsync_PairsTeamsByMatchupId()
    {
        var alpha = AddMembership("Alpha", "u1");
        var beta = AddMembership("Beta", "u2");
        _client.Rosters =
        [
            new SleeperRosterDto { RosterId = 1, OwnerId = "u1" },
            new SleeperRosterDto { RosterId = 2, OwnerId = "u2" },
        ];
        _client.MatchupsByWeek = new Dictionary<int, List<SleeperMatchupDto>>
        {
            [5] =
            [
                new SleeperMatchupDto { RosterId = 1, MatchupId = 100, Points = 0 },
                new SleeperMatchupDto { RosterId = 2, MatchupId = 100, Points = 0 },
            ],
        };

        var preview = await GetPreviewAsync();

        var entry = Assert.Single(preview!.Matchups);
        Assert.Equal("Alpha", entry.TeamA.TeamName);
        Assert.Equal(alpha.Id, entry.TeamA.LeagueMembershipId);
        Assert.Equal("Beta", entry.TeamB.TeamName);
        Assert.Equal(beta.Id, entry.TeamB.LeagueMembershipId);
    }

    [Fact]
    public async Task GetWeekPreviewAsync_SkipsByeWeekEntryWithNoOpponent()
    {
        var alpha = AddMembership("Alpha", "u1");
        _client.Rosters = [new SleeperRosterDto { RosterId = 1, OwnerId = "u1" }];
        _client.MatchupsByWeek = new Dictionary<int, List<SleeperMatchupDto>>
        {
            [5] = [new SleeperMatchupDto { RosterId = 1, MatchupId = 100, Points = 0 }],
        };

        var preview = await GetPreviewAsync();

        Assert.Empty(preview!.Matchups);
    }

    [Fact]
    public async Task GetWeekPreviewAsync_SkipsPairWhenARosterHasNoLinkedMembership()
    {
        AddMembership("Alpha", "u1");
        _client.Rosters =
        [
            new SleeperRosterDto { RosterId = 1, OwnerId = "u1" },
            new SleeperRosterDto { RosterId = 2, OwnerId = "unlinked-user" },
        ];
        _client.MatchupsByWeek = new Dictionary<int, List<SleeperMatchupDto>>
        {
            [5] =
            [
                new SleeperMatchupDto { RosterId = 1, MatchupId = 100, Points = 0 },
                new SleeperMatchupDto { RosterId = 2, MatchupId = 100, Points = 0 },
            ],
        };

        var preview = await GetPreviewAsync();

        Assert.Empty(preview!.Matchups);
    }

    [Fact]
    public async Task GetWeekPreviewAsync_OrdersEntriesDeterministically_ByTeamATeamName()
    {
        AddMembership("Zeta", "u1");
        AddMembership("ZetaOpponent", "u2");
        AddMembership("Alpha", "u3");
        AddMembership("AlphaOpponent", "u4");
        _client.Rosters =
        [
            new SleeperRosterDto { RosterId = 1, OwnerId = "u1" },
            new SleeperRosterDto { RosterId = 2, OwnerId = "u2" },
            new SleeperRosterDto { RosterId = 3, OwnerId = "u3" },
            new SleeperRosterDto { RosterId = 4, OwnerId = "u4" },
        ];
        _client.MatchupsByWeek = new Dictionary<int, List<SleeperMatchupDto>>
        {
            [5] =
            [
                new SleeperMatchupDto { RosterId = 1, MatchupId = 100, Points = 0 },
                new SleeperMatchupDto { RosterId = 2, MatchupId = 100, Points = 0 },
                new SleeperMatchupDto { RosterId = 3, MatchupId = 200, Points = 0 },
                new SleeperMatchupDto { RosterId = 4, MatchupId = 200, Points = 0 },
            ],
        };

        var preview = await GetPreviewAsync();

        Assert.Equal(["Alpha", "Zeta"], preview!.Matchups.Select(m => m.TeamA.TeamName));
    }
}
