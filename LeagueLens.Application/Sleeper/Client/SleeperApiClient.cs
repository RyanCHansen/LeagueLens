using System.Net.Http.Json;
using LeagueLens.Application.Sleeper.Client.Dtos;

namespace LeagueLens.Application.Sleeper.Client;

public sealed class SleeperApiClient(HttpClient httpClient) : ISleeperApiClient
{
    public const string BaseUrl = "https://api.sleeper.app/v1/";

    public async Task<SleeperNflStateDto> GetNflStateAsync(CancellationToken ct) =>
        await httpClient.GetFromJsonAsync<SleeperNflStateDto>("state/nfl", ct)
        ?? throw new InvalidOperationException("Sleeper returned an empty NFL state response.");

    public async Task<SleeperLeagueDto> GetLeagueAsync(string sleeperLeagueId, CancellationToken ct) =>
        await httpClient.GetFromJsonAsync<SleeperLeagueDto>($"league/{sleeperLeagueId}", ct)
        ?? throw new InvalidOperationException($"Sleeper league '{sleeperLeagueId}' was not found.");

    public async Task<IReadOnlyList<SleeperUserDto>> GetLeagueUsersAsync(string sleeperLeagueId, CancellationToken ct) =>
        await httpClient.GetFromJsonAsync<List<SleeperUserDto>>($"league/{sleeperLeagueId}/users", ct) ?? [];

    public async Task<IReadOnlyList<SleeperRosterDto>> GetLeagueRostersAsync(string sleeperLeagueId, CancellationToken ct) =>
        await httpClient.GetFromJsonAsync<List<SleeperRosterDto>>($"league/{sleeperLeagueId}/rosters", ct) ?? [];

    public async Task<IReadOnlyList<SleeperMatchupDto>> GetMatchupsAsync(string sleeperLeagueId, int week, CancellationToken ct) =>
        await httpClient.GetFromJsonAsync<List<SleeperMatchupDto>>($"league/{sleeperLeagueId}/matchups/{week}", ct) ?? [];

    public async Task<IReadOnlyDictionary<string, SleeperPlayerDto>> GetAllPlayersAsync(CancellationToken ct) =>
        await httpClient.GetFromJsonAsync<Dictionary<string, SleeperPlayerDto>>("players/nfl", ct) ?? [];
}
