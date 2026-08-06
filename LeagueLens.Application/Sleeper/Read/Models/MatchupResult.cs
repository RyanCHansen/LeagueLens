namespace LeagueLens.Application.Sleeper.Read.Models;

/// <summary>
/// One completed matchup, with real scores -- unlike <c>MatchupPreviewEntry</c> (the live current-week
/// pairing), this only ever covers weeks <c>SleeperSyncService</c> has actually persisted.
/// <c>TeamA</c>/<c>TeamB</c> are the same generic, neutral slot labels used there (stable sort by
/// team name then membership ID), not a real distinction -- see ADR-007.
/// </summary>
public sealed record MatchupResult(int Week, MatchupTeamResult TeamA, MatchupTeamResult TeamB);

public sealed record MatchupTeamResult(Guid LeagueMembershipId, string? TeamName, decimal Score);
