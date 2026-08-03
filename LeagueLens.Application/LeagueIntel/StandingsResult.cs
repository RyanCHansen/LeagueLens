namespace LeagueLens.Application.LeagueIntel;

/// <summary>The standings slice of a <see cref="LeagueIntelSummary"/>, with response metadata.</summary>
public sealed record StandingsResult(
    string SleeperLeagueId,
    int Season,
    int ThroughWeek,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<StandingsEntry> Standings);
