using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class ResourceRecordRepository(ApplicationDbContext context, IMeilisearchService meilisearchService)
    : IResourceRecordRepository
{
    public async Task<ICollection<ResourceRecord>> GetResourceRecordsAsync(List<string> order, List<bool> desc,
        int position, int take, Expression<Func<ResourceRecord, bool>>? predicate = null)
    {
        var result = context.ResourceRecords.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ResourceRecord> GetResourceRecordAsync(Guid id)
    {
        return (await context.ResourceRecords.FirstOrDefaultAsync(resourceRecord => resourceRecord.Id == id))!;
    }

    public async Task<bool> ResourceRecordExistsAsync(Guid id)
    {
        return await context.ResourceRecords.AnyAsync(resourceRecord => resourceRecord.Id == id);
    }

    public async Task<bool> CreateResourceRecordAsync(ResourceRecord resourceRecord)
    {
        await context.ResourceRecords.AddAsync(resourceRecord);
        await meilisearchService.AddAsync(resourceRecord);
        return await SaveAsync();
    }

    public async Task<bool> CreateResourceRecordsAsync(IEnumerable<ResourceRecord> resourceRecords)
    {
        var enumerable = resourceRecords.ToList();
        await context.ResourceRecords.AddRangeAsync(enumerable);
        await meilisearchService.AddBatchAsync(enumerable);
        return await SaveAsync();
    }

    public async Task<bool> UpdateResourceRecordAsync(ResourceRecord resourceRecord)
    {
        context.ResourceRecords.Update(resourceRecord);
        await meilisearchService.UpdateAsync(resourceRecord);
        return await SaveAsync();
    }

    public async Task<bool> RemoveResourceRecordAsync(Guid id)
    {
        context.ResourceRecords.Remove(
            (await context.ResourceRecords.FirstOrDefaultAsync(resourceRecord => resourceRecord.Id == id))!);
        await meilisearchService.DeleteAsync<ResourceRecord>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountResourceRecordsAsync(
        Expression<Func<ResourceRecord, bool>>? predicate = null)
    {
        var result = context.ResourceRecords.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}