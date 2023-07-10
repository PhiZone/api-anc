using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class RecordRepository : IRecordRepository
{
    private readonly ApplicationDbContext _context;

    public RecordRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Record>> GetRecordsAsync(string order, bool desc, int position, int take,
        Expression<Func<Record, bool>>? predicate = null)
    {
        var result = _context.Records.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<Record> GetRecordAsync(Guid id)
    {
        return (await _context.Records.FirstOrDefaultAsync(record => record.Id == id))!;
    }

    public async Task<bool> RecordExistsAsync(Guid id)
    {
        return await _context.Records.AnyAsync(record => record.Id == id);
    }

    public async Task<bool> CreateRecordAsync(Record record)
    {
        await _context.Records.AddAsync(record);
        return await SaveAsync();
    }

    public async Task<bool> UpdateRecordAsync(Record record)
    {
        _context.Records.Update(record);
        return await SaveAsync();
    }

    public async Task<bool> RemoveRecordAsync(Guid id)
    {
        _context.Records.Remove((await _context.Records.FirstOrDefaultAsync(record => record.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountRecordsAsync(Expression<Func<Record, bool>>? predicate = null)
    {
        var result = _context.Records.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}