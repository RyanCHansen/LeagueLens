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

        var matchupIds = matchups.Select(m => m.Id).ToList();
        var participants = await db.MatchupParticipants
            .Where(p => matchupIds.Contains(p.MatchupId))
            .ToListAsync(ct);
        var teamNames = await db.LeagueMemberships
            .Where(m => m.LeagueId == league.Id)
            .ToDictionaryAsync(m => m.Id, m => m.TeamName, ct);

        var participantsByMatchupId = participants.ToLookup(p => p.MatchupId);

        var entries = matchups
            .Where(m => participantsByMatchupId[m.Id].Count() == 2)
            .Select(m => ToRecapEntry(participantsByMatchupId[m.Id].ToList(), teamNames))
            .OrderBy(e => e.HomeTeamName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(e => e.HomeLeagueMembershipId)
            .ToList();

        var recap = new WeekRecapResult(sleeperLeagueId, league.Season, week, entries);
        return new WeekRecapLookupResult(LeagueExists: true, Recap: recap);
    }

    // The domain model has no concept of "home"/"away" (see ADR-007) -- a matchup is just two
    // participants. The Home/Away-shaped response below is a placeholder kept for this
    // milestone's compile/test stability only; picking a side deterministically (by team name,
    // then membership ID, matching the tie-break convention used elsewhere in this service) is
    // an API-layer display choice, not a persisted or business-meaningful distinction. This
    // response shape is expected to be redesigned in a follow-up milestone.
    private static MatchupRecapEntry ToRecapEntry(IReadOnlyList<MatchupParticipant> pair, IReadOnlyDictionary<Guid, string?> teamNames)
    {
        teamNames.TryGetValue(pair[0].LeagueMembershipId, out var firstName);
        teamNames.TryGetValue(pair[1].LeagueMembershipId, out var secondName);

        var firstIsHome = string.CompareOrdinal(firstName ?? string.Empty, secondName ?? string.Empty) < 0 ||
            (firstName == secondName && pair[0].LeagueMembershipId.CompareTo(pair[1].LeagueMembershipId) < 0);
        var (home, away) = firstIsHome ? (pair[0], pair[1]) : (pair[1], pair[0]);
        var (homeName, awayName) = firstIsHome ? (firstName, secondName) : (secondName, firstName);

        var winner = home.Score == away.Score
            ? MatchupWinner.Tie
            : home.Score > away.Score ? MatchupWinner.Home : MatchupWinner.Away;

        return new MatchupRecapEntry(
            home.LeagueMembershipId, homeName, home.Score,
            away.LeagueMembershipId, awayName, away.Score,
            Math.Abs(home.Score - away.Score), winner);
    }
}
