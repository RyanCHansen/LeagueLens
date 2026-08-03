namespace LeagueLens.Application.LeagueIntel;

/// <summary>The trends slice of a <see cref="LeagueIntelSummary"/>, with response metadata.</summary>
public sealed record TrendsResult(
    string SleeperLeagueId,
    int Season,
    int ThroughWeek,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<TrendEntry> Trends);
