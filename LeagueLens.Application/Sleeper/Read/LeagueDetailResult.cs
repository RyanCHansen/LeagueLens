namespace LeagueLens.Application.Sleeper.Read;

/// <summary>A league's identity and its current members. No rosters/matchups -- see the dedicated endpoints for those.</summary>
public sealed record LeagueDetailResult(
    string SleeperLeagueId,
    string Name,
    int Season,
    IReadOnlyList<LeagueMembershipSummary> Memberships);

public sealed record LeagueMembershipSummary(
    Guid LeagueMembershipId,
    string SleeperUserId,
    string? TeamName);
