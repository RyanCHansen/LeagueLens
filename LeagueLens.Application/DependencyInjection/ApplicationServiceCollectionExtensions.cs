using LeagueLens.Application.Sleeper.Client;
using LeagueLens.Application.Sleeper.Preview;
using LeagueLens.Application.Sleeper.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeagueLens.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddSleeperSync(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<ISleeperApiClient, SleeperApiClient>(client =>
            {
                client.BaseAddress = new Uri(SleeperApiClient.BaseUrl);
            })
            .AddStandardResilienceHandler();

        services.AddOptions<SleeperSyncOptions>()
            .Bind(configuration.GetSection(SleeperSyncOptions.SectionName));
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<LeagueSyncer>();
        services.AddScoped<PlayerCatalogSyncer>();
        services.AddScoped<LeagueMembershipSyncer>();
        services.AddScoped<RosterSyncer>();
        services.AddScoped<MatchupSyncer>();
        services.AddScoped<SleeperSyncService>();
        services.AddScoped<IWeekPreviewService, WeekPreviewService>();

        return services;
    }
}
