using LeagueLens.Domain.Entities;
using LeagueLens.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeagueLens.Application.LeagueIntel;

/// <inheritdoc cref="IWeekRecapService"/>
public sealed class WeekRecapService(LeagueLensDbContext db) : IWeekRecapService
{
    public async Task<WeekRecapLookupResult> GetWeekRecapAsync(string sleeperLeagueId, int week, CancellationToken ct)
    {
        var league = await db.Leagues.SingleOrDefaultAsync(l => l.SleeperLeagueId == sleeperLeagueId, ct);
        if (league is null)
            return new WeekRecapLookupResult(LeagueExists: false, Recap: null);

        var matchups = await db.Matchups
            .Where(m => m.LeagueId == league.Id && m.Week == week)
            .ToListAsync(ct);
        if (matchups.Count == 0)
            return new WeekRecapLookupResult(LeagueExists: true, Recap: null);

        var teamNames = await db.LeagueMemberships
            .Where(m => m.LeagueId == league.Id)
            .ToDictionaryAsync(m => m.Id, m => m.TeamName, ct);

        var entries = matchups
            .Select(m => ToRecapEntry(m, teamNames))
            .OrderBy(e => e.HomeTeamName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(e => e.HomeLeagueMembershipId)
            .ToList();

        var recap = new WeekRecapResult(sleeperLeagueId, league.Season, week, entries);
        return new WeekRecapLookupResult(LeagueExists: true, Recap: recap);
    }

    private static MatchupRecapEntry ToRecapEntry(Matchup matchup, IReadOnlyDictionary<Guid, string?> teamNames)
    {
        teamNames.TryGetValue(matchup.HomeLeagueMembershipId, out var homeTeamName);
        teamNames.TryGetValue(matchup.AwayLeagueMembershipId, out var awayTeamName);

        var winner = matchup.HomeScore == matchup.AwayScore
            ? MatchupWinner.Tie
            : matchup.HomeScore > matchup.AwayScore ? MatchupWinner.Home : MatchupWinner.Away;

        return new MatchupRecapEntry(
            matchup.HomeLeagueMembershipId, homeTeamName, matchup.HomeScore,
            matchup.AwayLeagueMembershipId, awayTeamName, matchup.AwayScore,
            Math.Abs(matchup.HomeScore - matchup.AwayScore), winner);
    }
}
