using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Constants;
using PhiZoneApi.Data;
using PhiZoneApi.Enums;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Services;

public class EventTaskScheduler(IServiceProvider serviceProvider, ILogger<EventTaskScheduler> logger)
    : IHostedService, IDisposable
{
    private CancellationToken _cancellationToken;
    private readonly Dictionary<Guid, Timer?> _schedules = new();

    public void Dispose()
    {
        foreach (var schedule in _schedules)
        {
            schedule.Value?.Dispose();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tasks = await context.EventTasks
            .Where(e => e.Type == TaskType.Scheduled && e.DateExecuted != null &&
                        e.DateExecuted > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);
        foreach (var task in tasks)
        {
            Schedule(task);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var schedule in _schedules)
        {
            schedule.Value?.Change(Timeout.Infinite, 0);
        }

        return Task.CompletedTask;
    }

    public void Schedule(EventTask task, bool replace = false)
    {
        if (_schedules.TryGetValue(task.Id, out var schedule))
        {
            if (!replace)
            {
                logger.LogInformation(LogEvents.SchedulerInfo, "[{Now}] Task \"{Name}\" is already scheduled",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), task.Name);
                return;
            }
            schedule?.Dispose();
            _schedules.Remove(task.Id);
        }

        _schedules.Add(task.Id,
            new Timer(Execute, task.Id, (task.DateExecuted! - DateTimeOffset.UtcNow).Value, Timeout.InfiniteTimeSpan));
        logger.LogInformation(LogEvents.SchedulerInfo, "[{Now}] Successfully scheduled Task \"{Name}\" at {Date}",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), task.Name, task.DateExecuted);
    }

    private async void Execute(object? state)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var scriptService = scope.ServiceProvider.GetRequiredService<ScriptService>();
        await scriptService.RunAsync<object>((Guid)state!, cancellationToken: _cancellationToken);
    }
}