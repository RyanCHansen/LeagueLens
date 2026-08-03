namespace LeagueLens.Application.LeagueIntel;

/// <summary>League-wide highlights for a single completed week, derived from that week's recap.</summary>
public sealed record WeekHighlightsResult(
    string SleeperLeagueId,
    int Season,
    int Week,
    WeekHighlightEntry HighestScoringTeam,
    WeekHighlightEntry ClosestGame,
    WeekHighlightEntry BiggestBlowout);
