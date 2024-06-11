using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class ServiceRecordRepository(ApplicationDbContext context)
    : IServiceRecordRepository
{
    public async Task<ICollection<ServiceRecord>> GetServiceRecordsAsync(List<string>? order,
        List<bool>? desc, int? position = 0, int? take = -1, Expression<Func<ServiceRecord, bool>>? predicate = null)
    {
        var result = context.ServiceRecords.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ServiceRecord> GetServiceRecordAsync(Guid id)
    {
        IQueryable<ServiceRecord> result = context.ServiceRecords;
        return (await result.FirstOrDefaultAsync(ServiceRecord => ServiceRecord.Id == id))!;
    }

    public async Task<bool> ServiceRecordExistsAsync(Guid id)
    {
        return await context.ServiceRecords.AnyAsync(ServiceRecord =>
            ServiceRecord.Id == id);
    }

    public async Task<bool> CreateServiceRecordAsync(ServiceRecord serviceRecord)
    {
        await context.ServiceRecords.AddAsync(serviceRecord);
        return await SaveAsync();
    }

    public async Task<bool> UpdateServiceRecordAsync(ServiceRecord serviceRecord)
    {
        context.ServiceRecords.Update(serviceRecord);
        return await SaveAsync();
    }

    public async Task<bool> RemoveServiceRecordAsync(Guid id)
    {
        context.ServiceRecords.Remove(
            (await context.ServiceRecords.FirstOrDefaultAsync(ServiceRecord =>
                ServiceRecord.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountServiceRecordsAsync(
        Expression<Func<ServiceRecord, bool>>? predicate = null)
    {
        var result = context.ServiceRecords.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}