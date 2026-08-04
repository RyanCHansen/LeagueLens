namespace LeagueLens.Application.Sleeper.Preview;

/// <summary>The current week's matchup pairings for a league, fetched live from Sleeper (see ADR-008) rather than persisted.</summary>
public sealed record WeekPreviewResult(int Week, IReadOnlyList<MatchupPreviewEntry> Matchups);
