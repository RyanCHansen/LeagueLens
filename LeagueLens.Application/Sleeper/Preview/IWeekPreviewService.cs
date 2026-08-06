using LeagueLens.Application.Sleeper.Preview.Models;

namespace LeagueLens.Application.Sleeper.Preview;

/// <summary>
/// Fetches the current week's matchup pairings live from Sleeper -- <c>SleeperSyncService</c>
/// never persists the current (in-progress/upcoming) week, so there's no local data to read this
/// from. See ADR-008 for why an on-demand live call here doesn't conflict with ADR-002's
/// rejection of continuous background live-game sync.
/// </summary>
public interface IWeekPreviewService
{
    /// <summary>Returns the current week's preview, or <see langword="null"/> if no league with the given Sleeper league ID has been synced.</summary>
    Task<WeekPreviewResult?> GetWeekPreviewAsync(string sleeperLeagueId, CancellationToken ct);
}
