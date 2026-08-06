namespace LeagueLens.Application.Sleeper.Sync.Models;

/// <summary>
/// One cooldown-gated step of an orchestrated sync. <see cref="Ran"/> is false when the step
/// was skipped because its cooldown hadn't elapsed yet -- <see cref="Result"/> is then null and
/// <see cref="LastSuccessfulSyncAt"/>/<see cref="NextSyncAvailableAt"/> describe the previous run.
/// </summary>
public sealed record SyncStepOutcome<TResult>(
    bool Ran,
    DateTimeOffset LastSuccessfulSyncAt,
    DateTimeOffset NextSyncAvailableAt,
    TResult? Result);

public sealed record LeagueSyncOrchestrationResult(
    SyncStepOutcome<PlayerCatalogSyncResult> PlayerCatalog,
    SyncStepOutcome<SyncResult> League);
