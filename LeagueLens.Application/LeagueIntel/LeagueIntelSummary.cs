namespace LeagueLens.Application.LeagueIntel;

/// <summary>
/// A composed, query-time read model combining standings, power rankings, and trends for a
/// league, plus the metadata every API response needs. Not persisted — see ADR-006.
/// </summary>
public sealed record LeagueIntelSummary(
    string SleeperLeagueId,
    int Season,
    int ThroughWeek,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<StandingsEntry> Standings,
    IReadOnlyList<PowerRankingEntry> PowerRankings,
    IReadOnlyList<TrendEntry> Trends)
{
    /// <summary>Reshapes this summary into just its standings slice, for the standings endpoint.</summary>
    public StandingsResult ToStandingsResult() =>
        new(SleeperLeagueId, Season, ThroughWeek, GeneratedAt, Standings);

    /// <summary>Reshapes this summary into just its power rankings slice, for the power rankings endpoint.</summary>
    public PowerRankingsResult ToPowerRankingsResult() =>
        new(SleeperLeagueId, Season, ThroughWeek, GeneratedAt, PowerRankings);

    /// <summary>Reshapes this summary into just its trends slice, for the trends endpoint.</summary>
    public TrendsResult ToTrendsResult() =>
        new(SleeperLeagueId, Season, ThroughWeek, GeneratedAt, Trends);
}
