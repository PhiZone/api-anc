using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenIddict.EntityFrameworkCore.Models;
using PhiZoneApi.Configurations;
using PhiZoneApi.Constants;
using PhiZoneApi.Data;
using StackExchange.Redis;

namespace PhiZoneApi.Services;

public class TokenPruningService(
    IServiceProvider serviceProvider,
    IConnectionMultiplexer multiplexer,
    IOptions<TokenPruningSettings> settingsAccessor,
    ILogger<TokenPruningService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = settingsAccessor.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation(LogEvents.TokenPruningInfo, "OpenIddict token pruning is disabled");
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(settings.InitialDelayMinutes), stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromHours(settings.IntervalHours));
            do
            {
                try
                {
                    await PruneAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    logger.LogError(LogEvents.TokenPruningFailure, e, "Failed to prune expired OpenIddict tokens");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task PruneAsync(CancellationToken cancellationToken)
    {
        var settings = settingsAccessor.Value;
        if (settings.BatchSize <= 0 || settings.MaxBatchesPerRun <= 0) return;

        var db = multiplexer.GetDatabase();
        const string lockKey = "phizone:lock:token-prune";
        var acquired = false;
        try
        {
            acquired = await db.StringSetAsync(lockKey, Environment.MachineName,
                TimeSpan.FromMinutes(settings.LockTtlMinutes), When.NotExists);
        }
        catch (Exception e)
        {
            logger.LogWarning(LogEvents.TokenPruningFailure, e,
                "Failed to acquire the token pruning lock in Redis; proceeding without it");
        }

        if (!acquired)
        {
            logger.LogInformation(LogEvents.TokenPruningInfo,
                "Skipped token pruning: another instance is already pruning");
            return;
        }

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.SetCommandTimeout(settings.CommandTimeoutSeconds);

            var cutoff = DateTime.UtcNow;
            var lastId = 0;
            var cleanBatches = 0;
            long deletedTotal = 0;

            for (var i = 0; i < settings.MaxBatchesPerRun && !cancellationToken.IsCancellationRequested; i++)
            {
                // Scan rows in primary key order so no full table scan is ever issued:
                // only a bounded window of rows is read and deleted per batch, which
                // keeps the load constant regardless of the table size.
                var window = await context.Set<OpenIddictEntityFrameworkCoreToken<int>>()
                    .Where(token => token.Id > lastId)
                    .OrderBy(token => token.Id)
                    .Take(settings.BatchSize)
                    .Select(token => new { token.Id, token.ExpirationDate })
                    .ToListAsync(cancellationToken);

                if (window.Count == 0) break;

                var windowFirstId = window[0].Id;
                lastId = window[^1].Id;

                if (window.All(row => row.ExpirationDate == null || row.ExpirationDate >= cutoff))
                {
                    if (++cleanBatches >= settings.StopAfterCleanBatches) break;
                }
                else
                {
                    cleanBatches = 0;
                    deletedTotal += await context.Set<OpenIddictEntityFrameworkCoreToken<int>>()
                        .Where(token => token.Id >= windowFirstId && token.Id <= lastId &&
                                       token.ExpirationDate != null && token.ExpirationDate < cutoff)
                        .ExecuteDeleteAsync(cancellationToken);
                }

                if (i < settings.MaxBatchesPerRun - 1)
                    await Task.Delay(TimeSpan.FromSeconds(settings.PauseBetweenBatchesSeconds), cancellationToken);
            }

            if (deletedTotal > 0)
            {
                logger.LogInformation(LogEvents.TokenPruningInfo,
                    "Pruned {Count} expired OpenIddict tokens", deletedTotal);
            }
        }
        finally
        {
            try
            {
                await db.KeyDeleteAsync(lockKey);
            }
            catch (Exception e)
            {
                logger.LogWarning(LogEvents.TokenPruningFailure, e,
                    "Failed to release the token pruning lock in Redis");
            }
        }
    }
}
