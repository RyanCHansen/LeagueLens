using System.Text.Json.Serialization;

namespace LeagueLens.Application.LeagueIntel;

/// <summary>Which side won a matchup, or whether it ended in a tie.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MatchupWinner
{
    [JsonStringEnumMemberName("home")]
    Home,

    [JsonStringEnumMemberName("away")]
    Away,

    [JsonStringEnumMemberName("tie")]
    Tie,
}
