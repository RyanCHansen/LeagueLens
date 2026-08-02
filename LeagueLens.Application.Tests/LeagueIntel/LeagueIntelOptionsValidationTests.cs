using LeagueLens.Application.DependencyInjection;
using LeagueLens.Application.LeagueIntel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LeagueLens.Application.Tests.LeagueIntel;

public class LeagueIntelOptionsValidationTests
{
    [Fact]
    public void ResolvingOptions_Throws_WhenPowerRankingWeightsDoNotSumToOne()
    {
        var services = new ServiceCollection();
        services.AddLeagueIntel();
        services.PostConfigure<LeagueIntelOptions>(o =>
        {
            o.PowerRankingWinPctWeight = 0.5m;
            o.PowerRankingPointsForWeight = 0.6m;
        });
        var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<LeagueIntelOptions>>().Value);
    }

    [Fact]
    public void ResolvingOptions_Succeeds_WithDefaultWeights()
    {
        var services = new ServiceCollection();
        services.AddLeagueIntel();
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<LeagueIntelOptions>>().Value;

        Assert.Equal(1.0m, options.PowerRankingWinPctWeight + options.PowerRankingPointsForWeight);
    }
}
