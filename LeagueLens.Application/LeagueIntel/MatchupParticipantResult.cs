namespace LeagueLens.Application.LeagueIntel;

/// <summary>One team's side of a matchup result: its score and how the matchup turned out for it.</summary>
public sealed record MatchupParticipantResult(
    Guid LeagueMembershipId,
    string? TeamName,
    decimal Score,
    MatchupOutcome Outcome);
