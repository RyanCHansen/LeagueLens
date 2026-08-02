using LeagueLens.Application.LeagueIntel;
using Microsoft.Extensions.Options;

namespace LeagueLens.Application.Tests.LeagueIntel;

public class BlendedPowerRankingCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsZero_WhenNoGamesPlayed()
    {
        var calculator = new BlendedPowerRankingCalculator(Options.Create(new LeagueIntelOptions()));

        var score = calculator.Calculate(wins: 0, ties: 0, gamesPlayed: 0, pointsFor: 0m, maxPointsForInLeague: 500m);

        Assert.Equal(0m, score);
    }

    [Fact]
    public void Calculate_BlendsWinPctAndNormalizedPointsFor_WithDefaultWeights()
    {
        var calculator = new BlendedPowerRankingCalculator(Options.Create(new LeagueIntelOptions()));

        // 3-1 record (win% = 0.75); points-for is half the league max.
        var score = calculator.Calculate(wins: 3, ties: 0, gamesPlayed: 4, pointsFor: 250m, maxPointsForInLeague: 500m);

        Assert.Equal(0.625m, score); // 0.5*0.75 + 0.5*0.5
    }

    [Fact]
    public void Calculate_TreatsTiesAsHalfWins()
    {
        var calculator = new BlendedPowerRankingCalculator(Options.Create(new LeagueIntelOptions()));

        var score = calculator.Calculate(wins: 1, ties: 2, gamesPlayed: 4, pointsFor: 0m, maxPointsForInLeague: 0m);

        Assert.Equal(0.25m, score); // winPct = (1 + 0.5*2)/4 = 0.5, pointsFor normalized = 0 -> 0.5*0.5 + 0.5*0
    }

    [Fact]
    public void Calculate_RespectsConfiguredWeights()
    {
        var options = Options.Create(new LeagueIntelOptions
        {
            PowerRankingWinPctWeight = 0.8m,
            PowerRankingPointsForWeight = 0.2m,
        });
        var calculator = new BlendedPowerRankingCalculator(options);

        var score = calculator.Calculate(wins: 4, ties: 0, gamesPlayed: 4, pointsFor: 500m, maxPointsForInLeague: 500m);

        Assert.Equal(1.0m, score); // 0.8*1 + 0.2*1
    }
}
