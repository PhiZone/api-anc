using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace PhiZoneApi.Services;

public class ScriptService(IServiceProvider serviceProvider) : IScriptService
{
    private readonly Dictionary<Guid, Script<EventTaskResponseDto?>> _eventTaskScripts = new();
    private readonly Dictionary<Guid, Script<ServiceResponseDto>> _serviceScripts = new();

    public async Task InitializeAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        foreach (var service in await context.ServiceScripts.ToListAsync(cancellationToken))
        {
            var script = CSharpScript.Create<ServiceResponseDto>(service.Code,
                ScriptOptions.Default.AddReferences(typeof(ServiceResponseDto).Assembly),
                GetGlobalsType(service.TargetType));
            script.Compile(cancellationToken);
            _serviceScripts.Add(service.Id, script);
        }

        foreach (var task in await context.EventTasks.Include(e => e.Division).ToListAsync(cancellationToken))
        {
            var script = CSharpScript.Create<EventTaskResponseDto?>(task.Code,
                ScriptOptions.Default.AddReferences(typeof(EventTaskResponseDto).Assembly),
                GetGlobalsType(task.Division.Type));
            script.Compile(cancellationToken);
            _eventTaskScripts.Add(task.Id, script);
        }
    }

    public async Task<ServiceResponseDto> RunAsync<T>(Guid id, T? target, Dictionary<string, string> parameters,
        User currentUser, CancellationToken? cancellationToken = null)
    {
        if (!_eventTaskScripts.ContainsKey(id))
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var serviceScriptRepository = scope.ServiceProvider.GetRequiredService<IServiceScriptRepository>();
            var service = await serviceScriptRepository.GetServiceScriptAsync(id);
            var script = CSharpScript.Create<ServiceResponseDto>(service.Code,
                ScriptOptions.Default.AddReferences(typeof(ServiceResponseDto).Assembly),
                GetGlobalsType(service.TargetType));
            script.Compile(cancellationToken ?? CancellationToken.None);
            _serviceScripts.Add(service.Id, script);
        }

        return (await _serviceScripts[id]
            .RunAsync(
                new ServiceScriptGlobals<T>
                {
                    Parameters = parameters,
                    Target = target,
                    CurrentUser = currentUser,
                    ServiceProvider = serviceProvider
                }, null, cancellationToken ?? CancellationToken.None)).ReturnValue;
    }

    public async Task<EventTaskResponseDto?> RunAsync<T>(Guid id, T? target = default, Guid? teamId = null,
        User? currentUser = null, CancellationToken? cancellationToken = null)
    {
        if (!_eventTaskScripts.ContainsKey(id))
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var eventTaskRepository = scope.ServiceProvider.GetRequiredService<IEventTaskRepository>();
            var task = await eventTaskRepository.GetEventTaskAsync(id);
            var script = CSharpScript.Create<EventTaskResponseDto?>(task.Code,
                ScriptOptions.Default.AddReferences(typeof(EventTaskResponseDto).Assembly),
                GetGlobalsType(task.Division.Type));
            script.Compile(cancellationToken ?? CancellationToken.None);
            _eventTaskScripts.Add(task.Id, script);
        }

        return (await _eventTaskScripts[id]
            .RunAsync(
                new EventTaskScriptGlobals<T>
                {
                    Target = target, TeamId = teamId, CurrentUser = currentUser, ServiceProvider = serviceProvider
                }, null, cancellationToken ?? CancellationToken.None)).ReturnValue;
    }

    public void Compile(Guid id, string code, ServiceTargetType type)
    {
        _serviceScripts[id] = CSharpScript.Create<ServiceResponseDto>(code,
            ScriptOptions.Default.AddReferences(typeof(ServiceResponseDto).Assembly), GetGlobalsType(type));
    }

    public void Compile(Guid id, string code, EventDivisionType type)
    {
        _eventTaskScripts[id] = CSharpScript.Create<EventTaskResponseDto?>(code,
            ScriptOptions.Default.AddReferences(typeof(EventTaskResponseDto).Assembly), GetGlobalsType(type));
    }

    public void RemoveServiceScript(Guid id)
    {
        _serviceScripts.Remove(id);
    }

    public void RemoveEventTaskScript(Guid id)
    {
        _serviceScripts.Remove(id);
    }

    public async Task<EventTaskResponseDto?> RunEventTaskAsync<T>(Guid divisionId, T target, Guid teamId,
        User currentUser, List<EventTaskType> types)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var eventTaskRepository = scope.ServiceProvider.GetRequiredService<IEventTaskRepository>();
        foreach (var task in await eventTaskRepository.GetEventTasksAsync(order: ["DateUpdated"],
                     predicate: e => e.DivisionId == divisionId && types.Contains(e.Type)))
        {
            var response = await RunAsync(task.Id, target, teamId, currentUser);
            if (response == null || response.Status == ResponseStatus.Ok) continue;
            return response;
        }

        return null;
    }

    private static Type GetGlobalsType(ServiceTargetType type)
    {
        return type switch
        {
            ServiceTargetType.SongSubmission => typeof(ServiceScriptGlobals<SongSubmission>),
            ServiceTargetType.ChartSubmission => typeof(ServiceScriptGlobals<ChartSubmission>),
            _ => typeof(ServiceScriptGlobals<object>)
        };
    }

    private static Type GetGlobalsType(EventDivisionType type)
    {
        return type switch
        {
            EventDivisionType.Song => typeof(EventTaskScriptGlobals<SongSubmission>),
            EventDivisionType.Chart => typeof(EventTaskScriptGlobals<ChartSubmission>),
            EventDivisionType.Play => typeof(EventTaskScriptGlobals<Record>),
            _ => typeof(EventTaskScriptGlobals<object>)
        };
    }

    public class ServiceScriptGlobals<T> : ScriptGlobals
    {
        public Dictionary<string, string> Parameters { get; set; } = null!;

        public T? Target { get; set; }

        public User CurrentUser { get; set; } = null!;
    }

    public class EventTaskScriptGlobals<T> : ScriptGlobals
    {
        public T? Target { get; set; }

        public Guid? TeamId { get; set; }

        public User? CurrentUser { get; set; }
    }

    public class ScriptGlobals
    {
        public IServiceProvider ServiceProvider { get; set; } = null!;
    }
}