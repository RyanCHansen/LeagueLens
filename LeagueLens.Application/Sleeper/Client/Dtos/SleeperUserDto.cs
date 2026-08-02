using System.Text.Json.Serialization;

namespace LeagueLens.Application.Sleeper.Client.Dtos;

public sealed class SleeperUserDto
{
    [JsonPropertyName("user_id")]
    public required string UserId { get; init; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("metadata")]
    public SleeperUserMetadataDto? Metadata { get; init; }
}

public sealed class SleeperUserMetadataDto
{
    [JsonPropertyName("team_name")]
    public string? TeamName { get; init; }
}
