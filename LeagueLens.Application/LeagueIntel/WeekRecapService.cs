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
            .OrderBy(e => e.TeamA.TeamName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(e => e.TeamA.LeagueMembershipId)
            .ToList();

        var recap = new WeekRecapResult(sleeperLeagueId, league.Season, week, entries);
        return new WeekRecapLookupResult(LeagueExists: true, Recap: recap);
    }

    private static MatchupRecapEntry ToRecapEntry(IReadOnlyList<MatchupParticipant> pair, IReadOnlyDictionary<Guid, string?> teamNames)
    {
        // Each participant gets its own outcome; a matchup has no privileged side. Which one
        // lands in the TeamA vs. TeamB slot is decided by team name (then membership ID) purely
        // for stable output -- the same tie-break convention used everywhere else in this API --
        // not to designate either side as primary.
        var results = new[]
        {
            new MatchupParticipantResult(pair[0].LeagueMembershipId, teamNames.GetValueOrDefault(pair[0].LeagueMembershipId), pair[0].Score, OutcomeFor(pair[0].Score, pair[1].Score)),
            new MatchupParticipantResult(pair[1].LeagueMembershipId, teamNames.GetValueOrDefault(pair[1].LeagueMembershipId), pair[1].Score, OutcomeFor(pair[1].Score, pair[0].Score)),
        };
        Array.Sort(results, (a, b) =>
        {
            var byName = string.CompareOrdinal(a.TeamName ?? string.Empty, b.TeamName ?? string.Empty);
            return byName != 0 ? byName : a.LeagueMembershipId.CompareTo(b.LeagueMembershipId);
        });

        return new MatchupRecapEntry(results[0], results[1], Math.Abs(pair[0].Score - pair[1].Score));
    }

    private static MatchupOutcome OutcomeFor(decimal score, decimal opponentScore) =>
        score == opponentScore ? MatchupOutcome.Tie : score > opponentScore ? MatchupOutcome.Win : MatchupOutcome.Loss;
}
