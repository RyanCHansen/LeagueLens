namespace LeagueLens.Application.LeagueIntel;

/// <summary>
/// Surfaces league-wide highlights (highest score, closest game, biggest blowout) for a single
/// completed week, computed from <see cref="IWeekRecapService"/>'s output rather than querying
/// persisted data directly -- <see cref="IWeekRecapService"/> stays the single source of truth
/// for completed-week matchup data.
/// </summary>
public interface IWeekHighlightsService
{
    /// <summary>
    /// Returns the requested week's highlights along with whether the league exists, so callers
    /// can distinguish "league not synced" from "week not synced" (both 404s, different reasons).
    /// </summary>
    Task<WeekHighlightsLookupResult> GetWeekHighlightsAsync(string sleeperLeagueId, int week, CancellationToken ct);
}
