using System.Diagnostics;
using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Application.Sleeper.Mapping;
using LeagueLens.Domain.Entities;
using LeagueLens.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeagueLens.Application.Sleeper.Sync;

public sealed class LeagueSyncer(LeagueLensDbContext db)
{
    public async Task<(SyncStats Stats, Guid LeagueId)> SyncAsync(SleeperLeagueDto dto, CancellationToken ct)
    {
        var persistQuery = Stopwatch.StartNew();
        var existing = await db.Leagues.SingleOrDefaultAsync(l => l.SleeperLeagueId == dto.LeagueId, ct);
        persistQuery.Stop();

        var map = Stopwatch.StartNew();
        League entity;
        var isNew = existing is null;
        if (existing is not null)
        {
            LeagueMapper.MapOnto(dto, existing);
            entity = existing;
        }
        else
        {
            entity = LeagueMapper.Map(dto);
        }
        map.Stop();

        var persistWrite = Stopwatch.StartNew();
        if (isNew) db.Leagues.Add(entity);
        persistWrite.Stop();

        var stats = new SyncStats(
            isNew ? 1 : 0, isNew ? 0 : 1, 0, map.Elapsed, persistQuery.Elapsed + persistWrite.Elapsed);
        return (stats, entity.Id);
    }
}
