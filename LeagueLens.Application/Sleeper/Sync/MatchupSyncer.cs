using System.Diagnostics;
using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Mapping;
using LeagueLens.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeagueLens.Application.Sleeper.Sync;

// Same replace-the-snapshot reasoning as RosterSyncer: a (League, Week) pairing has no stable
// per-row key from Sleeper (home/away order is arbitrary), so each sync clears and re-inserts
// that week's rows rather than attempting a field-level upsert.
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
        var existingRows = await db.Matchups
            .Where(m => m.LeagueId == leagueId && m.Week == week)
            .ToListAsync(ct);
        persistQuery.Stop();

        var map = Stopwatch.StartNew();
        var newRows = MatchupMapper.Map(weekMatchups, week, leagueId, membershipIdByRosterId, out var skipped);
        map.Stop();

        if (skipped > 0)
            warnings.Add($"Week {week}: {skipped} matchup entry(ies) skipped (bye week or unlinked roster)");

        var persistWrite = Stopwatch.StartNew();
        db.Matchups.RemoveRange(existingRows);
        db.Matchups.AddRange(newRows);
        persistWrite.Stop();

        return new SyncStats(newRows.Count, 0, skipped, map.Elapsed, persistQuery.Elapsed + persistWrite.Elapsed);
    }
}
