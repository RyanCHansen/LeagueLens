namespace LeagueLens.Application.Sleeper.Sync;

/// <summary>
/// Bound from the "SleeperSync" section of appsettings.json (see AddSleeperSync) -- to change
/// these values, edit that section, not the defaults below. The defaults here only take effect
/// if that config section is ever missing entirely; they exist so a missing section fails safe
/// (a real cooldown) rather than silently becoming 0 minutes (no cooldown at all).
/// </summary>
public sealed class SleeperSyncOptions
{
    public const string SectionName = "SleeperSync";

    public int PlayerCatalogCooldownMinutes { get; set; } = 60;
    public int LeagueSyncCooldownMinutes { get; set; } = 10;
}
