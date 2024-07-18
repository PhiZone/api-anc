using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Constants;
using PhiZoneApi.Data;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Services;

public class EventTaskScheduler(IServiceProvider serviceProvider, ILogger<EventTaskScheduler> logger)
    : IHostedService, IDisposable
{
    private readonly Dictionary<Guid, Timer?> _schedules = new();
    private CancellationToken _cancellationToken;

    public void Dispose()
    {
        foreach (var schedule in _schedules) schedule.Value?.Dispose();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tasks = await context.EventTasks
            .Where(e => e.Type == EventTaskType.Scheduled && e.Code != null && e.DateExecuted != null &&
                        e.DateExecuted > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);
        foreach (var task in tasks) Schedule(task);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var schedule in _schedules) schedule.Value?.Change(Timeout.Infinite, 0);

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

        if (task.DateExecuted == null) return;
        var delay = task.DateExecuted.Value - DateTimeOffset.UtcNow;
        if (delay.CompareTo(TimeSpan.Zero) < 0) return;

        _schedules.Add(task.Id, new Timer(Execute, (task.Id, task.DivisionId), delay, Timeout.InfiniteTimeSpan));
        logger.LogInformation(LogEvents.SchedulerInfo, "[{Now}] Successfully scheduled Task \"{Name}\" at {Date}",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), task.Name,
            task.DateExecuted.Value.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    public void ImplicitlySchedule(EventTask task, object? target, Guid? teamId, User? user,
        DateTimeOffset dateExecuted,
        bool replace = false)
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

        var delay = dateExecuted - DateTimeOffset.UtcNow;
        if (delay.CompareTo(TimeSpan.Zero) < 0) return;

        _schedules.Add(task.Id,
            new Timer(ImplicitlyExecute, (task.Id, target, teamId, user), delay, Timeout.InfiniteTimeSpan));
    }

    public void Cancel(Guid taskId)
    {
        if (_schedules.TryGetValue(taskId, out var schedule))
        {
            schedule?.Dispose();
            _schedules.Remove(taskId);
        }
    }

    private async void Execute(object? state)
    {
        var (id, divisionId) = (ValueTuple<Guid, Guid>)state!;
        await using var scope = serviceProvider.CreateAsyncScope();
        var scriptService = scope.ServiceProvider.GetRequiredService<IScriptService>();
        var eventDivisionRepository = scope.ServiceProvider.GetRequiredService<IEventDivisionRepository>();
        var eventDivision = await eventDivisionRepository.GetEventDivisionAsync(divisionId);
        await scriptService.RunAsync<object>(id, eventDivision, cancellationToken: _cancellationToken);
    }

    private async void ImplicitlyExecute(object? state)
    {
        var (id, target, teamId, user) = (ValueTuple<Guid, object?, Guid?, User?>)state!;
        await using var scope = serviceProvider.CreateAsyncScope();
        var scriptService = scope.ServiceProvider.GetRequiredService<IScriptService>();
        await scriptService.RunAsync(id, target, teamId, user, _cancellationToken);
    }
}