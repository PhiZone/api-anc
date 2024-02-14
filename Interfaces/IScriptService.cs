using PhiZoneApi.Data;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IScriptService
{
    Task InitializeAsync(ApplicationDbContext context, CancellationToken cancellationToken);

    Task<ServiceResponseDto> RunAsync<T>(Guid id, Dictionary<string, string> parameters, T target, User currentUser);

    Task RunAsync<T>(Guid id, T? target = default, User? currentUser = null);

    void Compile(Guid id, string code, ServiceTargetType type);

    void Compile(Guid id, string code, EventDivisionType type);

    void RemoveServiceScript(Guid id);

    void RemoveEventTaskScript(Guid id);
}