namespace LeagueLens.Application.LeagueIntel;

/// <summary>A single week highlight: a human-readable label paired with the matchup it refers to.</summary>
public sealed record WeekHighlightEntry(string Label, MatchupRecapEntry Matchup);
