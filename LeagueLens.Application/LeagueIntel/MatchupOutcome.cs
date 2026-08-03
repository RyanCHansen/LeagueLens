using System.Text.Json.Serialization;

namespace LeagueLens.Application.LeagueIntel;

/// <summary>How a single participant's matchup turned out.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MatchupOutcome
{
    [JsonStringEnumMemberName("win")]
    Win,

    [JsonStringEnumMemberName("loss")]
    Loss,

    [JsonStringEnumMemberName("tie")]
    Tie,
}
