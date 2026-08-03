namespace LeagueLens.Application.LeagueIntel;

/// <inheritdoc cref="IWeekHighlightsService"/>
public sealed class WeekHighlightsService(IWeekRecapService weekRecapService) : IWeekHighlightsService
{
    public async Task<WeekHighlightsLookupResult> GetWeekHighlightsAsync(string sleeperLeagueId, int week, CancellationToken ct)
    {
        var lookup = await weekRecapService.GetWeekRecapAsync(sleeperLeagueId, week, ct);
        if (!lookup.LeagueExists || lookup.Recap is null)
            return new WeekHighlightsLookupResult(lookup.LeagueExists, null);

        var matchups = lookup.Recap.Matchups;

        var highestScoringTeam = RankBy(matchups, m => Math.Max(m.TeamA.Score, m.TeamB.Score), descending: true);
        var closestGame = RankBy(matchups, m => m.MarginOfVictory, descending: false);
        var biggestBlowout = RankBy(matchups, m => m.MarginOfVictory, descending: true);

        var highlights = new WeekHighlightsResult(
            sleeperLeagueId, lookup.Recap.Season, week,
            new WeekHighlightEntry("Highest Scoring Team", highestScoringTeam),
            new WeekHighlightEntry("Closest Game", closestGame),
            new WeekHighlightEntry("Biggest Blowout", biggestBlowout));

        return new WeekHighlightsLookupResult(true, highlights);
    }

    // Same tie-break convention as everywhere else in this API (TeamA's name, then membership
    // ID) for every highlight, including HighestScoringTeam -- consistency over precision,
    // rather than tracking the actual record-holder for that one highlight specifically.
    private static MatchupRecapEntry RankBy(IReadOnlyList<MatchupRecapEntry> matchups, Func<MatchupRecapEntry, decimal> selector, bool descending)
    {
        var ordered = descending ? matchups.OrderByDescending(selector) : matchups.OrderBy(selector);
        return ordered
            .ThenBy(m => m.TeamA.TeamName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(m => m.TeamA.LeagueMembershipId)
            .First();
    }
}
