using System.Diagnostics;
using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Mapping;
using LeagueLens.Application.Sleeper.Sync.Models;
using LeagueLens.Domain.Entities;
using LeagueLens.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeagueLens.Application.Sleeper.Sync;

// Roster has no natural per-row unique key (a membership legitimately has many player rows
// per season), so it can't be field-level upserted like League/LeagueMembership/Player.
// Instead each sync replaces the full (LeagueMembershipId, Season) snapshot with the current
// one from Sleeper — correct and idempotent, since Roster only ever represents current state.
public sealed class RosterSyncer(LeagueLensDbContext db)
{
    public async Task<SyncStats> SyncAsync(
        IReadOnlyList<SleeperRosterDto> rosters,
        IReadOnlyList<string> leagueRosterPositions,
        IReadOnlyDictionary<string, Guid> membershipIdBySleeperUserId,
        int season,
        List<string> warnings,
        CancellationToken ct)
    {
        var allSleeperPlayerIds = rosters.SelectMany(r => r.Players ?? []).Distinct().ToList();
        var membershipIds = membershipIdBySleeperUserId.Values.ToList();

        var persistQuery = Stopwatch.StartNew();
        var playerIdBySleeperId = await db.Players
            .Where(p => allSleeperPlayerIds.Contains(p.SleeperPlayerId))
            .ToDictionaryAsync(p => p.SleeperPlayerId, p => p.Id, ct);
        var existingRosterRows = await db.Rosters
            .Where(r => membershipIds.Contains(r.LeagueMembershipId) && r.Season == season)
            .ToListAsync(ct);
        persistQuery.Stop();

        var map = Stopwatch.StartNew();
        var newRows = new List<Roster>();
        var skipped = 0;
        foreach (var roster in rosters)
        {
            if (roster.OwnerId is null || !membershipIdBySleeperUserId.TryGetValue(roster.OwnerId, out var membershipId))
            {
                warnings.Add($"Roster {roster.RosterId} has no linked membership; skipped");
                continue;
            }

            foreach (var (sleeperPlayerId, slot) in RosterMapper.MapSlots(roster, leagueRosterPositions))
            {
                if (!playerIdBySleeperId.TryGetValue(sleeperPlayerId, out var playerId))
                {
                    skipped++;
                    continue;
                }

                newRows.Add(new Roster
                {
                    Id = Guid.NewGuid(),
                    LeagueMembershipId = membershipId,
                    PlayerId = playerId,
                    Slot = slot,
                    Season = season,
                });
            }
        }
        map.Stop();

        if (skipped > 0)
            warnings.Add($"{skipped} roster slot(s) skipped: player not yet in the catalog or an unmapped starting position");

        var persistWrite = Stopwatch.StartNew();
        db.Rosters.RemoveRange(existingRosterRows);
        db.Rosters.AddRange(newRows);
        persistWrite.Stop();

        return new SyncStats(newRows.Count, 0, skipped, map.Elapsed, persistQuery.Elapsed + persistWrite.Elapsed);
    }
}
