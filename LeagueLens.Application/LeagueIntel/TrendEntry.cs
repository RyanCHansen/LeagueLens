namespace LeagueLens.Application.LeagueIntel;

/// <summary>
/// A team's recent-form indicators, comparing the current state against
/// <see cref="LeagueIntelOptions.TrendWindowWeeks"/> weeks ago. Both values are null until
/// enough weeks have been played to compute them.
/// </summary>
/// <param name="StandingMovement">Standings rank as of <c>TrendWindowWeeks</c> ago minus the current rank; positive means the team moved up.</param>
/// <param name="ScoringTrend">Average points-for over the last <c>TrendWindowWeeks</c> minus the average over the <c>TrendWindowWeeks</c> before that.</param>
public sealed record TrendEntry(
    Guid LeagueMembershipId,
    string? TeamName,
    int? StandingMovement,
    decimal? ScoringTrend);
