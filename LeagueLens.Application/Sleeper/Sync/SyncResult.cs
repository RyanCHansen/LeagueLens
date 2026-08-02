namespace LeagueLens.Application.Sleeper.Sync;

public sealed record SyncResult(
    string SleeperLeagueId,
    int Season,
    int ThroughWeek,
    SyncStats League,
    SyncStats Memberships,
    SyncStats Rosters,
    SyncStats Matchups,
    TimeSpan FetchDuration,
    TimeSpan MapDuration,
    TimeSpan PersistDuration,
    TimeSpan TotalDuration,
    IReadOnlyList<string> Warnings);

public sealed record PlayerCatalogSyncResult(
    SyncStats Players,
    TimeSpan FetchDuration,
    TimeSpan TotalDuration);
