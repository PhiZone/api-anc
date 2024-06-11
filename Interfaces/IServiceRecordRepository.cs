using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IServiceRecordRepository
{
    Task<ICollection<ServiceRecord>> GetServiceRecordsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0, int? take = -1, Expression<Func<ServiceRecord, bool>>? predicate = null);

    Task<ServiceRecord> GetServiceRecordAsync(Guid id);

    Task<bool> ServiceRecordExistsAsync(Guid id);

    Task<bool> CreateServiceRecordAsync(ServiceRecord serviceRecord);

    Task<bool> UpdateServiceRecordAsync(ServiceRecord serviceRecord);

    Task<bool> RemoveServiceRecordAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountServiceRecordsAsync(Expression<Func<ServiceRecord, bool>>? predicate = null);
}