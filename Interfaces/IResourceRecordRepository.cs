using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IResourceRecordRepository
{
    Task<ICollection<ResourceRecord>> GetResourceRecordsAsync(List<string> order, List<bool> desc, int position,
        int take,
        Expression<Func<ResourceRecord, bool>>? predicate = null);

    Task<ResourceRecord> GetResourceRecordAsync(Guid id);

    Task<bool> ResourceRecordExistsAsync(Guid id);

    Task<bool> CreateResourceRecordAsync(ResourceRecord resourceRecord);
    Task<bool> CreateResourceRecordsAsync(IEnumerable<ResourceRecord> resourceRecords);

    Task<bool> UpdateResourceRecordAsync(ResourceRecord resourceRecord);

    Task<bool> RemoveResourceRecordAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountResourceRecordsAsync(
        Expression<Func<ResourceRecord, bool>>? predicate = null);
}