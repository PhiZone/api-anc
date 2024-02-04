using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;
using Z.EntityFramework.Plus;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class ApplicationRepository(ApplicationDbContext context, IMeilisearchService meilisearchService)
    : IApplicationRepository
{
    public async Task<ICollection<Application>> GetApplicationsAsync(List<string> order, List<bool> desc, int position,
        int take,
        Expression<Func<Application, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.Applications.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.IncludeFilter(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Application> GetApplicationAsync(Guid id, int? currentUserId = null)
    {
        IQueryable<Application> result = context.Applications;
        if (currentUserId != null)
            result = result.IncludeFilter(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        return (await result.FirstOrDefaultAsync(application => application.Id == id))!;
    }

    public async Task<bool> ApplicationExistsAsync(Guid id)
    {
        return await context.Applications.AnyAsync(application => application.Id == id);
    }

    public async Task<bool> CreateApplicationAsync(Application application)
    {
        await context.Applications.AddAsync(application);
        await meilisearchService.AddAsync(application);
        return await SaveAsync();
    }

    public async Task<bool> UpdateApplicationAsync(Application application)
    {
        context.Applications.Update(application);
        await meilisearchService.UpdateAsync(application);
        return await SaveAsync();
    }

    public async Task<bool> RemoveApplicationAsync(Guid id)
    {
        context.Applications.Remove(
            (await context.Applications.FirstOrDefaultAsync(application => application.Id == id))!);
        await meilisearchService.DeleteAsync<Application>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountApplicationsAsync(
        Expression<Func<Application, bool>>? predicate = null)
    {
        var result = context.Applications.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}