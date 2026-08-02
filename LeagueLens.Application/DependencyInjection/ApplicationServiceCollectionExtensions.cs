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
}
