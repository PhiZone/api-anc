using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class CollectionRepository(ApplicationDbContext context, IMeilisearchService meilisearchService)
    : ICollectionRepository
{
    public async Task<ICollection<Collection>> GetCollectionsAsync(List<string> order, List<bool> desc, int position,
        int take, Expression<Func<Collection, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.Collections.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Collection> GetCollectionAsync(Guid id, int? currentUserId = null)
    {
        IQueryable<Collection> result = context.Collections;
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        return (await result.FirstOrDefaultAsync(collection => collection.Id == id))!;
    }

    public async Task<ICollection<Admission>> GetCollectionChartsAsync(Guid id, List<string> order, List<bool> desc,
        int position, int take, Expression<Func<Admission, bool>>? predicate = null)
    {
        var result = context.Admissions
            .Where(admission => admission.AdmitterId == id && admission.Status == RequestStatus.Approved)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<bool> CollectionExistsAsync(Guid id)
    {
        return (await context.Collections.AnyAsync(collection => collection.Id == id))!;
    }

    public async Task<bool> CreateCollectionAsync(Collection collection)
    {
        await context.Collections.AddAsync(collection);
        await meilisearchService.AddAsync(collection);
        return await SaveAsync();
    }

    public async Task<bool> UpdateCollectionAsync(Collection collection)
    {
        context.Collections.Update(collection);
        await meilisearchService.UpdateAsync(collection);
        return await SaveAsync();
    }

    public async Task<bool> RemoveCollectionAsync(Guid id)
    {
        context.Collections.Remove((await context.Collections.FirstOrDefaultAsync(collection => collection.Id == id))!);
        await meilisearchService.DeleteAsync<Collection>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountCollectionsAsync(Expression<Func<Collection, bool>>? predicate = null)
    {
        var result = context.Collections.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }

    public async Task<int> CountCollectionChartsAsync(Guid id, Expression<Func<Admission, bool>>? predicate = null)
    {
        var result = context.Admissions.Where(admission =>
            admission.AdmitterId == id && admission.Status == RequestStatus.Approved);
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}