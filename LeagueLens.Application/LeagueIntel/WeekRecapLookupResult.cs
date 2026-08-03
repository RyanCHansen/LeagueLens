namespace LeagueLens.Application.LeagueIntel;

/// <summary>
/// Result of a week-recap lookup, distinguishing "league not synced" from "week not synced"
/// in a single round trip so the controller can return a different 404 for each without a
/// separate existence check.
/// </summary>
/// <param name="LeagueExists">Whether a league with the requested Sleeper league ID has been synced.</param>
/// <param name="Recap">
/// The week's recap, or <see langword="null"/> if the league has no persisted matchup data
/// for the requested week (including when <paramref name="LeagueExists"/> is <see langword="false"/>).
/// </param>
public sealed record WeekRecapLookupResult(bool LeagueExists, WeekRecapResult? Recap);
