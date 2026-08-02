using System.Text.Json.Serialization;

namespace LeagueLens.Application.Sleeper.Client.Dtos;

public sealed class SleeperNflStateDto
{
    [JsonPropertyName("season")]
    public required string Season { get; init; }

    [JsonPropertyName("week")]
    public int Week { get; init; }
}
