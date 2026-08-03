namespace LeagueLens.Application.LeagueIntel;

/// <summary>
/// Looks up the recap of a single completed week for a league, from already-persisted
/// <see cref="Domain.Entities.Matchup"/> rows only — no live Sleeper calls, no new
/// persistence. Kept separate from <see cref="ILeagueIntelService"/> since a single-week
/// lookup has a different shape and responsibility than season-long standings/power-ranking/
/// trend aggregation.
/// </summary>
public interface IWeekRecapService
{
    /// <summary>
    /// Returns the requested week's recap along with whether the league exists, so callers
    /// can distinguish "league not synced" from "week not synced" (both 404s, different reasons).
    /// </summary>
    Task<WeekRecapLookupResult> GetWeekRecapAsync(string sleeperLeagueId, int week, CancellationToken ct);
}
