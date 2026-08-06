using System.Diagnostics;
using LeagueLens.Application.Sleeper.Client;
using LeagueLens.Application.Sleeper.Client.Dtos;
using LeagueLens.Persistence;
using Microsoft.Extensions.Logging;

namespace LeagueLens.Application.Sleeper.Sync;

// Orchestrates a full sync pass; the actual fetch/map/persist work for each entity type lives
// in its own Syncer so this class stays a thin pipeline (fetch -> delegate -> save -> log).
public sealed class SleeperSyncService(
    ISleeperApiClient client,
    LeagueLensDbContext db,
    LeagueSyncer leagueSyncer,
    LeagueMembershipSyncer membershipSyncer,
    RosterSyncer rosterSyncer,
    MatchupSyncer matchupSyncer,
    PlayerCatalogSyncer playerCatalogSyncer,
    ILogger<SleeperSyncService> logger)
{
    public async Task<PlayerCatalogSyncResult> SyncPlayerCatalogAsync(CancellationToken ct)
    {
        var total = Stopwatch.StartNew();

        var fetch = Stopwatch.StartNew();
        var players = await client.GetAllPlayersAsync(ct);
        fetch.Stop();

        var stats = await playerCatalogSyncer.SyncAsync(players, ct);
        var saveElapsed = await TimedSaveChangesAsync(ct);
        stats = stats with { PersistDuration = stats.PersistDuration + saveElapsed };

        total.Stop();
        var result = new PlayerCatalogSyncResult(stats, fetch.Elapsed, total.Elapsed);

        logger.LogInformation(
            "Sleeper player catalog sync: +{Added} ~{Updated} skipped {Skipped} in {TotalMs}ms " +
            "(fetch {FetchMs}ms, map {MapMs}ms, persist {PersistMs}ms)",
            result.Players.Added, result.Players.Updated, result.Players.Skipped,
            result.TotalDuration.TotalMilliseconds, result.FetchDuration.TotalMilliseconds,
            result.Players.MapDuration.TotalMilliseconds, result.Players.PersistDuration.TotalMilliseconds);

        return result;
    }

    public async Task<SyncResult> SyncLeagueAsync(string sleeperLeagueId, CancellationToken ct)
    {
        var total = Stopwatch.StartNew();
        var warnings = new List<string>();

        var fetch = Stopwatch.StartNew();
        var state = await client.GetNflStateAsync(ct);
        var leagueDto = await client.GetLeagueAsync(sleeperLeagueId, ct);
        var userDtos = await client.GetLeagueUsersAsync(sleeperLeagueId, ct);
        var rosterDtos = await client.GetLeagueRostersAsync(sleeperLeagueId, ct);

        var season = int.Parse(leagueDto.Season);
        // Only weeks strictly before the NFL's current week are treated as complete; the
        // in-progress week is skipped so partial/live scores aren't persisted as if final
        // (the current week's live pairing is served separately, on demand -- see ADR-008).
        var throughWeek = Math.Max(0, state.Week - 1);
        var matchupDtosByWeek = new Dictionary<int, IReadOnlyList<SleeperMatchupDto>>();
        for (var week = 1; week <= throughWeek; week++)
            matchupDtosByWeek[week] = await client.GetMatchupsAsync(sleeperLeagueId, week, ct);
        fetch.Stop();

        var (leagueStats, leagueId) = await leagueSyncer.SyncAsync(leagueDto, ct);
        var (membershipStats, membershipIdMap) = await membershipSyncer.SyncAsync(userDtos, leagueId, ct);
        var rosterStats = await rosterSyncer.SyncAsync(
            rosterDtos, leagueDto.RosterPositions, membershipIdMap, season, warnings, ct);

        var matchupStats = SyncStats.Empty;
        foreach (var (week, matchupDtos) in matchupDtosByWeek)
        {
            var weekStats = await matchupSyncer.SyncAsync(
                matchupDtos, rosterDtos, membershipIdMap, leagueId, week, warnings, ct);
            matchupStats = matchupStats.Combine(weekStats);
        }

        var saveElapsed = await TimedSaveChangesAsync(ct);
        var mapDuration = leagueStats.MapDuration + membershipStats.MapDuration + rosterStats.MapDuration + matchupStats.MapDuration;
        var persistDuration = leagueStats.PersistDuration + membershipStats.PersistDuration + rosterStats.PersistDuration + matchupStats.PersistDuration + saveElapsed;

        total.Stop();
        var result = new SyncResult(
            sleeperLeagueId, season, throughWeek,
            leagueStats, membershipStats, rosterStats, matchupStats,
            fetch.Elapsed, mapDuration, persistDuration, total.Elapsed, warnings);

        logger.LogInformation(
            "Sleeper sync for league {SleeperLeagueId} season {Season} through week {ThroughWeek}: " +
            "league(+{LeagueAdded}/~{LeagueUpdated}) memberships(+{MembershipsAdded}/~{MembershipsUpdated}) " +
            "rosters(+{RostersAdded}, skipped {RostersSkipped}) matchups(+{MatchupsAdded}, skipped {MatchupsSkipped}) " +
            "in {TotalMs}ms (fetch {FetchMs}ms, map {MapMs}ms, persist {PersistMs}ms)",
            result.SleeperLeagueId, result.Season, result.ThroughWeek,
            result.League.Added, result.League.Updated,
            result.Memberships.Added, result.Memberships.Updated,
            result.Rosters.Added, result.Rosters.Skipped,
            result.Matchups.Added, result.Matchups.Skipped,
            result.TotalDuration.TotalMilliseconds, result.FetchDuration.TotalMilliseconds,
            result.MapDuration.TotalMilliseconds, result.PersistDuration.TotalMilliseconds);

        if (result.Warnings.Count > 0)
            logger.LogWarning(
                "Sleeper sync for league {SleeperLeagueId} had {WarningCount} warning(s): {Warnings}",
                result.SleeperLeagueId, result.Warnings.Count, string.Join("; ", result.Warnings));

        return result;
    }

    private async Task<TimeSpan> TimedSaveChangesAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        await db.SaveChangesAsync(ct);
        sw.Stop();
        return sw.Elapsed;
    }
}
