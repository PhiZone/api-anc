using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IRecordRepository
{
    Task<ICollection<Record>> GetRecordsAsync(string order, bool desc, int position, int take,
        Expression<Func<Record, bool>>? predicate = null);

    Task<Record> GetRecordAsync(Guid id);

    Task<bool> RecordExistsAsync(Guid id);

    Task<bool> CreateRecordAsync(Record record);

    Task<bool> UpdateRecordAsync(Record record);

    Task<bool> UpdateRecordsAsync(IEnumerable<Record> records);

    Task<bool> RemoveRecordAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountRecordsAsync(Expression<Func<Record, bool>>? predicate = null);
}