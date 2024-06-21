using PhiZoneApi.Data;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IScriptService
{
    Task InitializeAsync(ApplicationDbContext context, CancellationToken cancellationToken);

    Task<ServiceResponseDto> RunAsync<T>(Guid id, T? target, Dictionary<string, string> parameters, User currentUser,
        CancellationToken? cancellationToken = null);

    Task<EventTaskResponseDto?> RunAsync<T>(Guid id, T? target = default, Guid? teamId = null, User? currentUser = null,
        CancellationToken? cancellationToken = null);

    void Compile(Guid id, string code, ServiceTargetType type);

    void Compile(Guid id, string code);

    void RemoveServiceScript(Guid id);

    void RemoveEventTaskScript(Guid id);

    Task<EventTaskResponseDto?> RunEventTaskAsync<T>(Guid divisionId, T target, Guid teamId, User currentUser,
        List<EventTaskType> types);
}