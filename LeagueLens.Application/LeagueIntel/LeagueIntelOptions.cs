namespace LeagueLens.Application.LeagueIntel;

/// <summary>
/// Configurable values for standings, power ranking, and trend calculations. Bound via the
/// standard <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/> pattern so these can
/// later come from configuration without changing any consumer.
/// </summary>
public sealed class LeagueIntelOptions
{
    /// <summary>Number of weeks used for trend calculations (standing movement and scoring trend).</summary>
    public int TrendWindowWeeks { get; set; } = 3;

    /// <summary>Weight applied to win percentage in the power ranking formula. Must sum to 1.0 with <see cref="PowerRankingPointsForWeight"/>.</summary>
    public decimal PowerRankingWinPctWeight { get; set; } = 0.5m;

    /// <summary>Weight applied to normalized points-for in the power ranking formula. Must sum to 1.0 with <see cref="PowerRankingWinPctWeight"/>.</summary>
    public decimal PowerRankingPointsForWeight { get; set; } = 0.5m;
}
