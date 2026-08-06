namespace LeagueLens.Application.Sleeper.Read.Models;

/// <summary>A page of player search results. <see cref="TotalCount"/> is the full match count, not just this page's size.</summary>
public sealed record PlayerSearchResult(
    IReadOnlyList<PlayerDetailResult> Players,
    int TotalCount,
    int Page,
    int PageSize);
