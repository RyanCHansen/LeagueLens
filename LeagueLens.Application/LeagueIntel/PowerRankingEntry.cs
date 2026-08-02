namespace LeagueLens.Application.LeagueIntel;

/// <summary>A team's computed power ranking score and rank within its league.</summary>
public sealed record PowerRankingEntry(
    Guid LeagueMembershipId,
    string? TeamName,
    decimal Score,
    int Rank);
