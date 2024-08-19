using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface ITapGhostService
{
    Task<TapGhost?> GetGhost(Guid appId, string id);

    Task<IEnumerable<Record>?> GetRecords(Guid appId, string id);

    Task<HttpResponseMessage> ModifyGhost(TapGhost ghost);

    Task<double> CreateRecord(Guid appId, string id, Record record);
}