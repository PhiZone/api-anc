using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class ResourceRecordRepository(ApplicationDbContext context) : IResourceRecordRepository
{
    public async Task<ICollection<ResourceRecord>> GetResourceRecordsAsync(List<string> order, List<bool> desc,
        int position, int take, string? search = null, Expression<Func<ResourceRecord, bool>>? predicate = null)
    {
        var result = context.ResourceRecords.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(resourceRecord => EF.Functions.Like(resourceRecord.Title.ToUpper(), search) ||
                                                    EF.Functions.Like(resourceRecord.AuthorName.ToUpper(), search) ||
                                                    (resourceRecord.Description != null &&
                                                     EF.Functions.Like(resourceRecord.Description.ToUpper(), search)) ||
                                                    EF.Functions.Like(resourceRecord.Source.ToUpper(), search) ||
                                                    EF.Functions.Like(resourceRecord.CopyrightOwner.ToUpper(), search));
        }

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
        return await SaveAsync();
    }

    public async Task<bool> UpdateResourceRecordAsync(ResourceRecord resourceRecord)
    {
        context.ResourceRecords.Update(resourceRecord);
        return await SaveAsync();
    }

    public async Task<bool> RemoveResourceRecordAsync(Guid id)
    {
        context.ResourceRecords.Remove(
            (await context.ResourceRecords.FirstOrDefaultAsync(resourceRecord => resourceRecord.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountResourceRecordsAsync(string? search = null,
        Expression<Func<ResourceRecord, bool>>? predicate = null)
    {
        var result = context.ResourceRecords.AsQueryable();

        if (predicate != null) result = result.Where(predicate);
        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(resourceRecord => EF.Functions.Like(resourceRecord.Title.ToUpper(), search) ||
                                                    EF.Functions.Like(resourceRecord.AuthorName.ToUpper(), search) ||
                                                    (resourceRecord.Description != null &&
                                                     EF.Functions.Like(resourceRecord.Description.ToUpper(), search)) ||
                                                    EF.Functions.Like(resourceRecord.Source.ToUpper(), search) ||
                                                    EF.Functions.Like(resourceRecord.CopyrightOwner.ToUpper(), search));
        }

        return await result.CountAsync();
    }
}