using LeagueLens.Application.Sleeper.Read.Models;
using LeagueLens.Domain.Entities;
using LeagueLens.Domain.Enums;
using LeagueLens.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeagueLens.Application.Sleeper.Read;

/// <inheritdoc cref="IPlayerReadService"/>
public sealed class PlayerReadService(LeagueLensDbContext db) : IPlayerReadService
{
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

    public async Task<PlayerDetailResult?> GetPlayerAsync(string sleeperPlayerId, CancellationToken ct)
    {
        var player = await db.Players.SingleOrDefaultAsync(p => p.SleeperPlayerId == sleeperPlayerId, ct);
        return player is null ? null : ToResult(player);
    }

    public async Task<PlayerSearchResult> SearchPlayersAsync(
        string? search, PlayerPosition? position, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var query = db.Players.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Explicit ToLower() on both sides rather than a bare .Contains(search) -- EF's
            // SQLite provider (used in tests) translates plain string.Contains to instr(), which
            // is case-sensitive, unlike SQL Server's default collation. Lower-casing both sides
            // makes the match case-insensitive on either provider instead of relying on an
            // implicit, provider-specific collation assumption.
            var lowerSearch = search.ToLower();
            query = query.Where(p => (p.FirstName + " " + p.LastName).ToLower().Contains(lowerSearch));
        }

        if (position is not null)
            query = query.Where(p => p.Position == position);

        var totalCount = await query.CountAsync(ct);

        var players = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PlayerSearchResult(players.Select(ToResult).ToList(), totalCount, page, pageSize);
    }

    private static PlayerDetailResult ToResult(Player player) =>
        new(player.SleeperPlayerId, player.FirstName, player.LastName, player.Position, player.NflTeam);
}
