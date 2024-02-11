using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class ApplicationServiceRecordRepository(ApplicationDbContext context)
    : IApplicationServiceRecordRepository
{
    public async Task<ICollection<ApplicationServiceRecord>> GetApplicationServiceRecordsAsync(List<string> order,
        List<bool> desc, int position, int take, Expression<Func<ApplicationServiceRecord, bool>>? predicate = null)
    {
        var result = context.ApplicationServiceRecords.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ApplicationServiceRecord> GetApplicationServiceRecordAsync(Guid id)
    {
        IQueryable<ApplicationServiceRecord> result = context.ApplicationServiceRecords;
        return (await result.FirstOrDefaultAsync(applicationServiceRecord => applicationServiceRecord.Id == id))!;
    }

    public async Task<bool> ApplicationServiceRecordExistsAsync(Guid id)
    {
        return await context.ApplicationServiceRecords.AnyAsync(applicationServiceRecord =>
            applicationServiceRecord.Id == id);
    }

    public async Task<bool> CreateApplicationServiceRecordAsync(ApplicationServiceRecord applicationServiceRecord)
    {
        await context.ApplicationServiceRecords.AddAsync(applicationServiceRecord);
        return await SaveAsync();
    }

    public async Task<bool> UpdateApplicationServiceRecordAsync(ApplicationServiceRecord applicationServiceRecord)
    {
        context.ApplicationServiceRecords.Update(applicationServiceRecord);
        return await SaveAsync();
    }

    public async Task<bool> RemoveApplicationServiceRecordAsync(Guid id)
    {
        context.ApplicationServiceRecords.Remove(
            (await context.ApplicationServiceRecords.FirstOrDefaultAsync(applicationServiceRecord =>
                applicationServiceRecord.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountApplicationServiceRecordsAsync(
        Expression<Func<ApplicationServiceRecord, bool>>? predicate = null)
    {
        var result = context.ApplicationServiceRecords.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}