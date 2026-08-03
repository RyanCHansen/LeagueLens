using LeagueLens.Application.Sleeper.Client;
using LeagueLens.Application.Sleeper.Client.Dtos;

namespace LeagueLens.Server.Tests.TestSupport;

// Program.cs wires a real SleeperApiClient pointed at the live Sleeper API via AddSleeperSync().
// Registered in place of it for controller tests so hitting an endpoint that calls Sleeper
// (e.g. the live preview) doesn't make a real network call.
public sealed class FakeSleeperApiClient : ISleeperApiClient
{
    public SleeperNflStateDto NflState { get; set; } = new() { Season = "2026", Week = 1 };
    public SleeperLeagueDto League { get; set; } = null!;
    public List<SleeperUserDto> Users { get; set; } = [];
    public List<SleeperRosterDto> Rosters { get; set; } = [];
    public Dictionary<int, List<SleeperMatchupDto>> MatchupsByWeek { get; set; } = [];
    public Dictionary<string, SleeperPlayerDto> Players { get; set; } = [];

    /// <summary>When set, every call throws this instead of returning data -- simulates Sleeper being unreachable.</summary>
    public Exception? ThrowOnCall { get; set; }

    public Task<SleeperNflStateDto> GetNflStateAsync(CancellationToken ct) =>
        ThrowOnCall is null ? Task.FromResult(NflState) : Task.FromException<SleeperNflStateDto>(ThrowOnCall);

    public Task<SleeperLeagueDto> GetLeagueAsync(string sleeperLeagueId, CancellationToken ct) =>
        ThrowOnCall is null ? Task.FromResult(League) : Task.FromException<SleeperLeagueDto>(ThrowOnCall);

    public Task<IReadOnlyList<SleeperUserDto>> GetLeagueUsersAsync(string sleeperLeagueId, CancellationToken ct) =>
        ThrowOnCall is null
            ? Task.FromResult<IReadOnlyList<SleeperUserDto>>(Users)
            : Task.FromException<IReadOnlyList<SleeperUserDto>>(ThrowOnCall);

    public Task<IReadOnlyList<SleeperRosterDto>> GetLeagueRostersAsync(string sleeperLeagueId, CancellationToken ct) =>
        ThrowOnCall is null
            ? Task.FromResult<IReadOnlyList<SleeperRosterDto>>(Rosters)
            : Task.FromException<IReadOnlyList<SleeperRosterDto>>(ThrowOnCall);

    public Task<IReadOnlyList<SleeperMatchupDto>> GetMatchupsAsync(string sleeperLeagueId, int week, CancellationToken ct) =>
        ThrowOnCall is null
            ? Task.FromResult<IReadOnlyList<SleeperMatchupDto>>(MatchupsByWeek.GetValueOrDefault(week, []))
            : Task.FromException<IReadOnlyList<SleeperMatchupDto>>(ThrowOnCall);

    public Task<IReadOnlyDictionary<string, SleeperPlayerDto>> GetAllPlayersAsync(CancellationToken ct) =>
        ThrowOnCall is null
            ? Task.FromResult<IReadOnlyDictionary<string, SleeperPlayerDto>>(Players)
            : Task.FromException<IReadOnlyDictionary<string, SleeperPlayerDto>>(ThrowOnCall);
}
