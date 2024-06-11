using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class ServiceScriptRepository(ApplicationDbContext context)
    : IServiceScriptRepository
{
    public async Task<ICollection<ServiceScript>> GetServiceScriptsAsync(List<string>? order = null,
        List<bool>? desc = null,
        int? position = 0, int? take = -1, Expression<Func<ServiceScript, bool>>? predicate = null,
        int? currentUserId = null)
    {
        var result = context.ServiceScripts.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ServiceScript> GetServiceScriptAsync(Guid id, int? currentUserId = null)
    {
        return (await context.ServiceScripts.FirstOrDefaultAsync(serviceScript => serviceScript.Id == id))!;
    }

    public async Task<bool> ServiceScriptExistsAsync(Guid id)
    {
        return await context.ServiceScripts.AnyAsync(serviceScript => serviceScript.Id == id);
    }

    public async Task<bool> CreateServiceScriptAsync(ServiceScript serviceScript)
    {
        await context.ServiceScripts.AddAsync(serviceScript);
        return await SaveAsync();
    }

    public async Task<bool> UpdateServiceScriptAsync(ServiceScript serviceScript)
    {
        context.ServiceScripts.Update(serviceScript);
        return await SaveAsync();
    }

    public async Task<bool> RemoveServiceScriptAsync(Guid id)
    {
        context.ServiceScripts.Remove(
            (await context.ServiceScripts.FirstOrDefaultAsync(serviceScript =>
                serviceScript.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountServiceScriptsAsync(Expression<Func<ServiceScript, bool>>? predicate = null)
    {
        var result = context.ServiceScripts.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}