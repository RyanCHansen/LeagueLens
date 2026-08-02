namespace LeagueLens.Application.LeagueIntel;

/// <summary>A team's season record and scoring totals, ranked within its league.</summary>
public sealed record StandingsEntry(
    Guid LeagueMembershipId,
    string? TeamName,
    int Wins,
    int Losses,
    int Ties,
    decimal PointsFor,
    decimal PointsAgainst,
    int Rank);
