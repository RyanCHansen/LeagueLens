using System.Diagnostics;
using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Mapping;
using LeagueLens.Application.Sleeper.Sync.Models;
using LeagueLens.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeagueLens.Application.Sleeper.Sync;

// Same replace-the-snapshot reasoning as RosterSyncer: a (League, Week) pairing has no stable
// per-row key from Sleeper (the two sides of a matchup are unordered), so each sync clears and
// re-inserts that week's rows rather than attempting a field-level upsert. Matchup has no
// navigation property to its MatchupParticipant rows (this codebase doesn't use EF navigation
// properties -- see other entities), so the old participants for a removed matchup have to be
// queried and removed explicitly; relying on the DB-level cascade FK alone wouldn't clear them
// from the change tracker before SaveChanges.
public sealed class MatchupSyncer(LeagueLensDbContext db)
{
    public async Task<SyncStats> SyncAsync(
        IReadOnlyList<SleeperMatchupDto> weekMatchups,
        IReadOnlyList<SleeperRosterDto> rosters,
        IReadOnlyDictionary<string, Guid> membershipIdBySleeperUserId,
        Guid leagueId,
        int week,
        List<string> warnings,
        CancellationToken ct)
    {
        var membershipIdByRosterId = rosters
            .Where(r => r.OwnerId is not null && membershipIdBySleeperUserId.ContainsKey(r.OwnerId))
            .ToDictionary(r => r.RosterId, r => membershipIdBySleeperUserId[r.OwnerId!]);

        var persistQuery = Stopwatch.StartNew();
        var existingMatchups = await db.Matchups
            .Where(m => m.LeagueId == leagueId && m.Week == week)
            .ToListAsync(ct);
        var existingMatchupIds = existingMatchups.Select(m => m.Id).ToList();
        var existingParticipants = await db.MatchupParticipants
            .Where(p => existingMatchupIds.Contains(p.MatchupId))
            .ToListAsync(ct);
        persistQuery.Stop();

        var map = Stopwatch.StartNew();
        var result = MatchupMapper.Map(weekMatchups, week, leagueId, membershipIdByRosterId, out var skipped);
        map.Stop();

        if (skipped > 0)
            warnings.Add($"Week {week}: {skipped} matchup entry(ies) skipped (bye week or unlinked roster)");

        var persistWrite = Stopwatch.StartNew();
        db.MatchupParticipants.RemoveRange(existingParticipants);
        db.Matchups.RemoveRange(existingMatchups);
        db.Matchups.AddRange(result.Matchups);
        db.MatchupParticipants.AddRange(result.Participants);
        persistWrite.Stop();

        return new SyncStats(result.Matchups.Count, 0, skipped, map.Elapsed, persistQuery.Elapsed + persistWrite.Elapsed);
    }
}
