using LeagueLens.Application.Sleeper.Client;
using LeagueLens.Application.Sleeper.Client.Dtos;

namespace LeagueLens.Application.Tests.TestSupport;

public sealed class FakeSleeperApiClient : ISleeperApiClient
{
    public SleeperNflStateDto NflState { get; set; } = new() { Season = "2026", Week = 1 };
    public SleeperLeagueDto League { get; set; } = null!;
    public List<SleeperUserDto> Users { get; set; } = [];
    public List<SleeperRosterDto> Rosters { get; set; } = [];
    public Dictionary<int, List<SleeperMatchupDto>> MatchupsByWeek { get; set; } = [];
    public Dictionary<string, SleeperPlayerDto> Players { get; set; } = [];

    public Task<SleeperNflStateDto> GetNflStateAsync(CancellationToken ct) => Task.FromResult(NflState);

    public Task<SleeperLeagueDto> GetLeagueAsync(string sleeperLeagueId, CancellationToken ct) => Task.FromResult(League);

    public Task<IReadOnlyList<SleeperUserDto>> GetLeagueUsersAsync(string sleeperLeagueId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SleeperUserDto>>(Users);

    public Task<IReadOnlyList<SleeperRosterDto>> GetLeagueRostersAsync(string sleeperLeagueId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SleeperRosterDto>>(Rosters);

    public Task<IReadOnlyList<SleeperMatchupDto>> GetMatchupsAsync(string sleeperLeagueId, int week, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SleeperMatchupDto>>(MatchupsByWeek.GetValueOrDefault(week, []));

    public Task<IReadOnlyDictionary<string, SleeperPlayerDto>> GetAllPlayersAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyDictionary<string, SleeperPlayerDto>>(Players);
}
