using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IApplicationServiceRecordRepository
{
    Task<ICollection<ApplicationServiceRecord>> GetApplicationServiceRecordsAsync(List<string> order, List<bool> desc,
        int position, int take, Expression<Func<ApplicationServiceRecord, bool>>? predicate = null);

    Task<ApplicationServiceRecord> GetApplicationServiceRecordAsync(Guid id);

    Task<bool> ApplicationServiceRecordExistsAsync(Guid id);

    Task<bool> CreateApplicationServiceRecordAsync(ApplicationServiceRecord applicationServiceRecord);

    Task<bool> UpdateApplicationServiceRecordAsync(ApplicationServiceRecord applicationServiceRecord);

    Task<bool> RemoveApplicationServiceRecordAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountApplicationServiceRecordsAsync(Expression<Func<ApplicationServiceRecord, bool>>? predicate = null);
}