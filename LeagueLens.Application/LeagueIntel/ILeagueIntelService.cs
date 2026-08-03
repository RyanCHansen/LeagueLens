namespace LeagueLens.Application.LeagueIntel;

/// <summary>
/// Computes standings, power rankings, and trends for a league. Abstracted behind an
/// interface so callers (e.g. controllers) depend on a stable contract, and so
/// cross-cutting behavior (caching, logging, metrics) can be added later as a decorator
/// without touching consumers.
/// </summary>
public interface ILeagueIntelService
{
    /// <summary>
    /// Loads league data once and computes standings, power rankings, and trends together.
    /// Returns <see langword="null"/> if no league with the given Sleeper league ID has been synced.
    /// </summary>
    Task<LeagueIntelSummary?> GetLeagueIntelAsync(string sleeperLeagueId, CancellationToken ct);
}
