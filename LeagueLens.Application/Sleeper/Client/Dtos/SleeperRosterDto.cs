using System.Text.Json.Serialization;

namespace LeagueLens.Application.Sleeper.Client.Dtos;

public sealed class SleeperRosterDto
{
    [JsonPropertyName("roster_id")]
    public int RosterId { get; init; }

    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; init; }

    [JsonPropertyName("players")]
    public List<string>? Players { get; init; }

    [JsonPropertyName("starters")]
    public List<string>? Starters { get; init; }

    [JsonPropertyName("reserve")]
    public List<string>? Reserve { get; init; }

    [JsonPropertyName("taxi")]
    public List<string>? Taxi { get; init; }
}
