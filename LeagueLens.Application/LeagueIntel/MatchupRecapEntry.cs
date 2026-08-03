namespace LeagueLens.Application.LeagueIntel;

/// <summary>The final result of a single matchup within a week's recap.</summary>
public sealed record MatchupRecapEntry(
    Guid HomeLeagueMembershipId,
    string? HomeTeamName,
    decimal HomeScore,
    Guid AwayLeagueMembershipId,
    string? AwayTeamName,
    decimal AwayScore,
    decimal MarginOfVictory,
    MatchupWinner Winner);
