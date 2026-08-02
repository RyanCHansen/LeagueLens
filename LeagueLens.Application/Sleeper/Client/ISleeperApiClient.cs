using LeagueLens.Application.Sleeper.Client.Dtos;

namespace LeagueLens.Application.Sleeper.Client;

public interface ISleeperApiClient
{
    Task<SleeperNflStateDto> GetNflStateAsync(CancellationToken ct);
    Task<SleeperLeagueDto> GetLeagueAsync(string sleeperLeagueId, CancellationToken ct);
    Task<IReadOnlyList<SleeperUserDto>> GetLeagueUsersAsync(string sleeperLeagueId, CancellationToken ct);
    Task<IReadOnlyList<SleeperRosterDto>> GetLeagueRostersAsync(string sleeperLeagueId, CancellationToken ct);
    Task<IReadOnlyList<SleeperMatchupDto>> GetMatchupsAsync(string sleeperLeagueId, int week, CancellationToken ct);
    Task<IReadOnlyDictionary<string, SleeperPlayerDto>> GetAllPlayersAsync(CancellationToken ct);
}
