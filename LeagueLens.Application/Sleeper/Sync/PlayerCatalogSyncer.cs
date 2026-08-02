using System.Diagnostics;
using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Mapping;
using LeagueLens.Domain.Entities;
using LeagueLens.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeagueLens.Application.Sleeper.Sync;

public sealed class PlayerCatalogSyncer(LeagueLensDbContext db, ILogger<PlayerCatalogSyncer> logger)
{
    public async Task<SyncStats> SyncAsync(IReadOnlyDictionary<string, SleeperPlayerDto> players, CancellationToken ct)
    {
        var persistQuery = Stopwatch.StartNew();
        var existingBySleeperId = await db.Players.ToDictionaryAsync(p => p.SleeperPlayerId, ct);
        persistQuery.Stop();

        var map = Stopwatch.StartNew();
        var toAdd = new List<Player>();
        var updated = 0;
        var skipped = 0;
        foreach (var (sleeperPlayerId, dto) in players)
        {
            if (existingBySleeperId.TryGetValue(sleeperPlayerId, out var existingPlayer))
            {
                if (PlayerMapper.TryMapPosition(dto.Position, out _))
                {
                    PlayerMapper.MapOnto(dto, existingPlayer);
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }
            else if (PlayerMapper.TryMap(sleeperPlayerId, dto, out var newPlayer))
            {
                toAdd.Add(newPlayer);
            }
            else
            {
                skipped++;
            }
        }
        map.Stop();

        var persistWrite = Stopwatch.StartNew();
        db.Players.AddRange(toAdd);
        persistWrite.Stop();

        if (skipped > 0)
            logger.LogDebug(
                "Player catalog sync skipped {SkippedCount} entries with unmapped position or missing name",
                skipped);

        return new SyncStats(toAdd.Count, updated, skipped, map.Elapsed, persistQuery.Elapsed + persistWrite.Elapsed);
    }
}
