using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IRecordRepository
{
    Task<ICollection<Record>> GetRecordsAsync(List<string>? order = null, List<bool>? desc = null, int? position = 0,
        int? take = -1,
        Expression<Func<Record, bool>>? predicate = null, bool queryChart = false, int? currentUserId = null);

    Task<Record> GetRecordAsync(Guid id, bool queryChart = false, int? currentUserId = null);

    Task<bool> RecordExistsAsync(Guid id);

    Task<bool> CreateRecordAsync(Record record);

    Task<bool> UpdateRecordAsync(Record record);

    Task<bool> UpdateRecordsAsync(IEnumerable<Record> records);

    Task<bool> RemoveRecordAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountRecordsAsync(Expression<Func<Record, bool>>? predicate = null);

    Task<ICollection<Record>> GetPersonalBests(int ownerId, int take = 19, bool queryChart = false,
        int? currentUserId = null);
}