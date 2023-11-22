using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class RecordRepository(ApplicationDbContext context) : IRecordRepository
{
    public async Task<ICollection<Record>> GetRecordsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Record, bool>>? predicate = null)
    {
        var result = context.Records.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Record> GetRecordAsync(Guid id)
    {
        return (await context.Records.FirstOrDefaultAsync(record => record.Id == id))!;
    }

    public async Task<bool> RecordExistsAsync(Guid id)
    {
        return await context.Records.AnyAsync(record => record.Id == id);
    }

    public async Task<bool> CreateRecordAsync(Record record)
    {
        await context.Records.AddAsync(record);
        return await SaveAsync();
    }

    public async Task<bool> UpdateRecordAsync(Record record)
    {
        context.Records.Update(record);
        return await SaveAsync();
    }

    public async Task<bool> UpdateRecordsAsync(IEnumerable<Record> records)
    {
        context.Records.UpdateRange(records);
        return await SaveAsync();
    }

    public async Task<bool> RemoveRecordAsync(Guid id)
    {
        context.Records.Remove((await context.Records.FirstOrDefaultAsync(record => record.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountRecordsAsync(Expression<Func<Record, bool>>? predicate = null)
    {
        var result = context.Records.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}