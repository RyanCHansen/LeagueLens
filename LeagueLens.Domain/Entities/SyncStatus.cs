using LeagueLens.Domain.Enums;

namespace LeagueLens.Domain.Entities;

public class SyncStatus
{
    public Guid Id { get; set; }
    public SyncProvider Provider { get; set; }
    public SyncKind SyncType { get; set; }

    /// <summary>Empty string for provider/sync-type combinations with no per-league scope (e.g. the player catalog).</summary>
    public required string SleeperLeagueId { get; set; }
    public DateTimeOffset LastSuccessfulSyncAt { get; set; }
}
