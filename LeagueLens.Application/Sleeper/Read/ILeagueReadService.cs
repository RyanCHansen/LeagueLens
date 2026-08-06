using LeagueLens.Application.Sleeper.Read.Models;

namespace LeagueLens.Application.Sleeper.Read;

/// <summary>
/// Reads already-persisted Sleeper data -- no live Sleeper calls, unlike <c>IWeekPreviewService</c>.
/// Every method returns <see langword="null"/> if no league with the given Sleeper league ID has
/// been synced, matching <c>IWeekPreviewService.GetWeekPreviewAsync</c>'s convention. A synced
/// league with no rosters/matchups yet returns an empty list, not <see langword="null"/> --
/// that's a valid state, not a not-found condition.
/// </summary>
public interface ILeagueReadService
{
    Task<LeagueDetailResult?> GetLeagueDetailAsync(string sleeperLeagueId, CancellationToken ct);
    Task<IReadOnlyList<TeamRosterResult>?> GetRostersAsync(string sleeperLeagueId, CancellationToken ct);
    Task<IReadOnlyList<MatchupResult>?> GetMatchupsAsync(string sleeperLeagueId, CancellationToken ct);
}
