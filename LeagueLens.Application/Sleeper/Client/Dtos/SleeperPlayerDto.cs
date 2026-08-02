using System.Text.Json.Serialization;

namespace LeagueLens.Application.Sleeper.Client.Dtos;

public sealed class SleeperPlayerDto
{
    [JsonPropertyName("first_name")]
    public string? FirstName { get; init; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; init; }

    [JsonPropertyName("position")]
    public string? Position { get; init; }

    [JsonPropertyName("team")]
    public string? Team { get; init; }
}
