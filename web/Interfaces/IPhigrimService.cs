using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Responses;

namespace PhiZoneApi.Interfaces;

public interface IPhigrimService
{
    Task<PhigrimInheritanceDto?> GetInheritingUser(TapTapGhostInheritanceDelivererDto dto);

    Task<ResponseDto<IEnumerable<RecordDto>>?> GetRecords(int remoteId, int? page = null, int? perPage = null);
}