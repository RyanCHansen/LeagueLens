using LeagueLens.Application.Sleeper.Read.Models;
using LeagueLens.Domain.Enums;

namespace LeagueLens.Application.Sleeper.Read;

/// <summary>Reads already-persisted player catalog data -- no live Sleeper calls, same as <see cref="ILeagueReadService"/>.</summary>
public interface IPlayerReadService
{
    /// <summary>Returns the player, or <see langword="null"/> if no player with the given Sleeper player ID has been synced.</summary>
    Task<PlayerDetailResult?> GetPlayerAsync(string sleeperPlayerId, CancellationToken ct);

    /// <summary>
    /// <paramref name="search"/> is a case-insensitive substring match against the player's full
    /// name (first + last); <paramref name="position"/> is an optional exact filter.
    /// <paramref name="page"/>/<paramref name="pageSize"/> are clamped to sane bounds (page &gt;= 1,
    /// 1 &lt;= pageSize &lt;= 100) rather than erroring on out-of-range input.
    /// </summary>
    Task<PlayerSearchResult> SearchPlayersAsync(
        string? search, PlayerPosition? position, int page, int pageSize, CancellationToken ct);
}
