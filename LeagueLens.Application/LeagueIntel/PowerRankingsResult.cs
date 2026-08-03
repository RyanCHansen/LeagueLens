namespace LeagueLens.Application.LeagueIntel;

/// <summary>The power rankings slice of a <see cref="LeagueIntelSummary"/>, with response metadata.</summary>
public sealed record PowerRankingsResult(
    string SleeperLeagueId,
    int Season,
    int ThroughWeek,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<PowerRankingEntry> PowerRankings);
