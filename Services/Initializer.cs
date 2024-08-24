using PhiZoneApi.Constants;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;

// ReSharper disable InvertIf

namespace PhiZoneApi.Services;

public class Initializer(IServiceProvider serviceProvider, ILogger<Initializer> logger) : IHostedService
{
    private CancellationToken _cancellationToken;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        Initialize();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async void Initialize()
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            logger.LogInformation(LogEvents.InitializerInfo, "Initializing leaderboards");
            var leaderboardService = scope.ServiceProvider.GetRequiredService<ILeaderboardService>();
            await leaderboardService.InitializeAsync(context, _cancellationToken);
            logger.LogInformation(LogEvents.InitializerInfo, "Initializing scripts");
            var scriptService = scope.ServiceProvider.GetRequiredService<IScriptService>();
            await scriptService.InitializeAsync(context, _cancellationToken);
            logger.LogInformation(LogEvents.InitializerInfo, "Initialization completed");
        }
        catch (Exception e)
        {
            logger.LogWarning(LogEvents.DataConsistencyMaintenance, e,
                "An error has occurred whilst checking for data consistency");
        }
    }
}