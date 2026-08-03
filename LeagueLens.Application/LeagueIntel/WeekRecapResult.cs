namespace LeagueLens.Application.LeagueIntel;

/// <summary>The recap for a single completed week: final matchup results for the league.</summary>
public sealed record WeekRecapResult(
    string SleeperLeagueId,
    int Season,
    int Week,
    IReadOnlyList<MatchupRecapEntry> Matchups);
