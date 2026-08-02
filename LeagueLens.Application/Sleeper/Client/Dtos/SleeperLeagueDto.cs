using System.Text.Json.Serialization;

namespace LeagueLens.Application.Sleeper.Client.Dtos;

public sealed class SleeperLeagueDto
{
    [JsonPropertyName("league_id")]
    public required string LeagueId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("season")]
    public required string Season { get; init; }

    [JsonPropertyName("roster_positions")]
    public List<string> RosterPositions { get; init; } = [];
}
