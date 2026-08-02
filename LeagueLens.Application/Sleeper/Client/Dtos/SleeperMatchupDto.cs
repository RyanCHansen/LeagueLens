using System.Text.Json.Serialization;

namespace LeagueLens.Application.Sleeper.Client.Dtos;

public sealed class SleeperMatchupDto
{
    [JsonPropertyName("roster_id")]
    public int RosterId { get; init; }

    [JsonPropertyName("matchup_id")]
    public int? MatchupId { get; init; }

    [JsonPropertyName("points")]
    public double Points { get; init; }
}
