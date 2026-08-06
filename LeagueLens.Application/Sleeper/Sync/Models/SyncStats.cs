namespace LeagueLens.Application.Sleeper.Sync.Models;

public sealed record SyncStats(int Added, int Updated, int Skipped, TimeSpan MapDuration, TimeSpan PersistDuration)
{
    public static readonly SyncStats Empty = new(0, 0, 0, TimeSpan.Zero, TimeSpan.Zero);

    public SyncStats Combine(SyncStats other) => new(
        Added + other.Added,
        Updated + other.Updated,
        Skipped + other.Skipped,
        MapDuration + other.MapDuration,
        PersistDuration + other.PersistDuration);
}
