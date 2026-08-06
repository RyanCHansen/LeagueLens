namespace LeagueLens.Application.Sleeper.Preview.Models;

/// <summary>A team's identity within a matchup preview -- no score, since the matchup hasn't been played yet.</summary>
public sealed record TeamPreviewInfo(Guid LeagueMembershipId, string? TeamName);
