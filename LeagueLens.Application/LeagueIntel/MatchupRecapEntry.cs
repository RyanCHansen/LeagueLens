namespace LeagueLens.Application.LeagueIntel;

/// <summary>
/// The final result of a single matchup within a week's recap. <c>TeamA</c>/<c>TeamB</c> are
/// generic, neutral slot labels assigned by a stable display sort (team name, then membership
/// ID) purely so repeated requests serialize identically -- they carry no real-world meaning
/// (no home/away, no seeding, no significance to which slot a team lands in).
/// </summary>
public sealed record MatchupRecapEntry(
    MatchupParticipantResult TeamA,
    MatchupParticipantResult TeamB,
    decimal MarginOfVictory);
