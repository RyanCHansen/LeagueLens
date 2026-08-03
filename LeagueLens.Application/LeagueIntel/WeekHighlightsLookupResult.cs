namespace LeagueLens.Application.LeagueIntel;

/// <summary>
/// Result of a week-highlights lookup, distinguishing "league not synced" from "week not
/// synced" -- mirrors <see cref="WeekRecapLookupResult"/>, since highlights are derived
/// entirely from a week's recap and propagate its not-found semantics rather than
/// reimplementing them.
/// </summary>
/// <param name="LeagueExists">Whether a league with the requested Sleeper league ID has been synced.</param>
/// <param name="Highlights">
/// The week's highlights, or <see langword="null"/> if the league has no persisted matchup
/// data for the requested week (including when <paramref name="LeagueExists"/> is <see langword="false"/>).
/// </param>
public sealed record WeekHighlightsLookupResult(bool LeagueExists, WeekHighlightsResult? Highlights);
