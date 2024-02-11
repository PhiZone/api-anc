using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace PhiZoneApi.Services;

public class ScriptService(IServiceProvider serviceProvider) : IScriptService
{
    private readonly Dictionary<Guid, Script> _eventTaskScripts = new();
    private readonly Dictionary<Guid, Script<ServiceResponseDto>> _serviceScripts = new();

    public async Task Initialize(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        foreach (var service in await context.ApplicationServices.ToListAsync(cancellationToken))
        {
            var script =
                CSharpScript.Create<ServiceResponseDto>(service.Code, ScriptOptions.Default
                    .AddReferences(typeof(ServiceResponseDto).Assembly), GetGlobalsType(service.TargetType));
            script.Compile(cancellationToken);
            _serviceScripts.Add(service.Id, script);
        }

        foreach (var task in await context.EventTasks.Include(e => e.Division).ToListAsync(cancellationToken))
        {
            var script =
                CSharpScript.Create(task.Code, globalsType: GetGlobalsType(task.Division.Type));
            script.Compile(cancellationToken);
            _eventTaskScripts.Add(task.Id, script);
        }
    }

    public async Task<ServiceResponseDto> RunAsync<T>(Guid id, Dictionary<string, string> parameters, T target,
        User currentUser)
    {
        return (await _serviceScripts[id].RunAsync(new ServiceScriptGlobals<T>
        {
            Parameters = parameters,
            Target = target,
            CurrentUser = currentUser,
            ServiceProvider = serviceProvider
        })).ReturnValue;
    }

    public async Task RunAsync<T>(Guid id, T? target = default, User? currentUser = null)
    {
        await _eventTaskScripts[id].RunAsync(new EventTaskScriptGlobals<T>
        {
            Target = target,
            CurrentUser = currentUser,
            ServiceProvider = serviceProvider
        });
    }

    public void Compile(Guid id, string code, ServiceTargetType type)
    {
        _serviceScripts[id] = CSharpScript.Create<ServiceResponseDto>(code, ScriptOptions.Default
            .AddReferences(typeof(ServiceResponseDto).Assembly), GetGlobalsType(type));
    }

    public void Compile(Guid id, string code, EventDivisionType type)
    {
        _eventTaskScripts[id] = CSharpScript.Create(code, globalsType: GetGlobalsType(type));
    }

    public void RemoveServiceScript(Guid id)
    {
        _serviceScripts.Remove(id);
    }

    public void RemoveEventTaskScript(Guid id)
    {
        _serviceScripts.Remove(id);
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
            EventDivisionType.Song => typeof(ServiceScriptGlobals<SongSubmission>),
            EventDivisionType.Chart => typeof(ServiceScriptGlobals<ChartSubmission>),
            EventDivisionType.Play => typeof(ServiceScriptGlobals<Record>),
            _ => typeof(ServiceScriptGlobals<object>)
        };
    }

    public class ServiceScriptGlobals<T> : ScriptGlobals
    {
        public Dictionary<string, string> Parameters { get; set; } = null!;

        public T Target { get; set; } = default!;

        public User CurrentUser { get; set; } = null!;
    }

    public class EventTaskScriptGlobals<T> : ScriptGlobals
    {
        public T? Target { get; set; }

        public User? CurrentUser { get; set; }
    }

    public class ScriptGlobals
    {
        public IServiceProvider ServiceProvider { get; set; } = null!;
    }
}