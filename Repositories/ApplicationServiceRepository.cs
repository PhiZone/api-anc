using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class ApplicationServiceRepository(ApplicationDbContext context)
    : IApplicationServiceRepository
{
    public async Task<ICollection<ApplicationService>> GetApplicationServicesAsync(List<string> order, List<bool> desc,
        int position, int take, Expression<Func<ApplicationService, bool>>? predicate = null)
    {
        var result = context.ApplicationServices.Include(e => e.Application).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ApplicationService> GetApplicationServiceAsync(Guid id)
    {
        IQueryable<ApplicationService> result = context.ApplicationServices.Include(e => e.Application);
        return (await result.FirstOrDefaultAsync(applicationService => applicationService.Id == id))!;
    }

    public async Task<bool> ApplicationServiceExistsAsync(Guid id)
    {
        return await context.ApplicationServices.AnyAsync(applicationService => applicationService.Id == id);
    }

    public async Task<bool> CreateApplicationServiceAsync(ApplicationService applicationService)
    {
        await context.ApplicationServices.AddAsync(applicationService);
        return await SaveAsync();
    }

    public async Task<bool> UpdateApplicationServiceAsync(ApplicationService applicationService)
    {
        context.ApplicationServices.Update(applicationService);
        return await SaveAsync();
    }

    public async Task<bool> RemoveApplicationServiceAsync(Guid id)
    {
        context.ApplicationServices.Remove(
            (await context.ApplicationServices.FirstOrDefaultAsync(applicationService =>
                applicationService.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountApplicationServicesAsync(Expression<Func<ApplicationService, bool>>? predicate = null)
    {
        var result = context.ApplicationServices.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}