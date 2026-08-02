using Microsoft.Extensions.Options;

namespace LeagueLens.Application.LeagueIntel;

/// <summary>
/// Blends win percentage and points-for (normalized against the league's highest points-for)
/// using the weights in <see cref="LeagueIntelOptions"/>.
/// </summary>
public sealed class BlendedPowerRankingCalculator(IOptions<LeagueIntelOptions> options) : IPowerRankingCalculator
{
    public decimal Calculate(int wins, int ties, int gamesPlayed, decimal pointsFor, decimal maxPointsForInLeague)
    {
        if (gamesPlayed == 0)
            return 0m;

        var config = options.Value;
        var winPct = (wins + 0.5m * ties) / gamesPlayed;
        var normalizedPointsFor = maxPointsForInLeague == 0 ? 0m : pointsFor / maxPointsForInLeague;

        return config.PowerRankingWinPctWeight * winPct + config.PowerRankingPointsForWeight * normalizedPointsFor;
    }
}
