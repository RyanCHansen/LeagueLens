namespace LeagueLens.Application.LeagueIntel;

/// <summary>
/// One matchup pairing for the current week. <c>TeamA</c>/<c>TeamB</c> are generic, neutral slot
/// labels assigned by a stable display sort (team name, then membership ID) -- same convention
/// as <see cref="MatchupRecapEntry"/> -- not a real distinction.
/// </summary>
public sealed record MatchupPreviewEntry(TeamPreviewInfo TeamA, TeamPreviewInfo TeamB);
