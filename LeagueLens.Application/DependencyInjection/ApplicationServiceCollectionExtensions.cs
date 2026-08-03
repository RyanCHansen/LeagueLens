using LeagueLens.Application.LeagueIntel;
using LeagueLens.Application.Sleeper.Client;
using LeagueLens.Application.Sleeper.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace LeagueLens.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddSleeperSync(this IServiceCollection services)
    {
        services.AddHttpClient<ISleeperApiClient, SleeperApiClient>(client =>
            {
                client.BaseAddress = new Uri(SleeperApiClient.BaseUrl);
            })
            .AddStandardResilienceHandler();

        services.AddScoped<LeagueSyncer>();
        services.AddScoped<PlayerCatalogSyncer>();
        services.AddScoped<LeagueMembershipSyncer>();
        services.AddScoped<RosterSyncer>();
        services.AddScoped<MatchupSyncer>();
        services.AddScoped<SleeperSyncService>();

        return services;
    }

    public static IServiceCollection AddLeagueIntel(this IServiceCollection services)
    {
        services.AddOptions<LeagueIntelOptions>()
            .Validate(
                o => o.PowerRankingWinPctWeight + o.PowerRankingPointsForWeight == 1.0m,
                $"{nameof(LeagueIntelOptions.PowerRankingWinPctWeight)} and {nameof(LeagueIntelOptions.PowerRankingPointsForWeight)} must sum to 1.0.")
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPowerRankingCalculator, BlendedPowerRankingCalculator>();
        services.AddScoped<ILeagueIntelService, LeagueIntelService>();

        return services;
    }
}
