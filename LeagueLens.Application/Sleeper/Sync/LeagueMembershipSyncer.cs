using System.Diagnostics;
using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Mapping;
using LeagueLens.Application.Sleeper.Sync.Models;
using LeagueLens.Domain.Entities;
using LeagueLens.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeagueLens.Application.Sleeper.Sync;

public sealed class LeagueMembershipSyncer(LeagueLensDbContext db)
{
    public async Task<(SyncStats Stats, IReadOnlyDictionary<string, Guid> MembershipIdBySleeperUserId)> SyncAsync(
        IReadOnlyList<SleeperUserDto> users, Guid leagueId, CancellationToken ct)
    {
        var persistQuery = Stopwatch.StartNew();
        var existingBySleeperUserId = await db.LeagueMemberships
            .Where(m => m.LeagueId == leagueId)
            .ToDictionaryAsync(m => m.SleeperUserId, ct);
        persistQuery.Stop();

        var map = Stopwatch.StartNew();
        var idMap = new Dictionary<string, Guid>();
        var toAdd = new List<LeagueMembership>();
        var updated = 0;
        foreach (var dto in users)
        {
            LeagueMembership membership;
            if (existingBySleeperUserId.TryGetValue(dto.UserId, out var existing))
            {
                LeagueMembershipMapper.MapOnto(dto, existing);
                membership = existing;
                updated++;
            }
            else
            {
                membership = LeagueMembershipMapper.Map(dto, leagueId);
                toAdd.Add(membership);
            }
            idMap[dto.UserId] = membership.Id;
        }
        map.Stop();

        var persistWrite = Stopwatch.StartNew();
        db.LeagueMemberships.AddRange(toAdd);
        persistWrite.Stop();

        var stats = new SyncStats(
            toAdd.Count, updated, 0, map.Elapsed, persistQuery.Elapsed + persistWrite.Elapsed);
        return (stats, idMap);
    }
}
