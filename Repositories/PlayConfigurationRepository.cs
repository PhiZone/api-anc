using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class PlayConfigurationRepository(ApplicationDbContext context) : IPlayConfigurationRepository
{
    public async Task<ICollection<PlayConfiguration>> GetPlayConfigurationsAsync(List<string>? order = null,
        List<bool>? desc = null,
        int? position = 0,
        int? take = -1,
        Expression<Func<PlayConfiguration, bool>>? predicate = null)
    {
        var result = context.PlayConfigurations.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<PlayConfiguration> GetPlayConfigurationAsync(Guid id)
    {
        return (await context.PlayConfigurations.FirstOrDefaultAsync(configuration => configuration.Id == id))!;
    }

    public async Task<bool> PlayConfigurationExistsAsync(Guid id)
    {
        return await context.PlayConfigurations.AnyAsync(configuration => configuration.Id == id);
    }

    public async Task<bool> CreatePlayConfigurationAsync(PlayConfiguration configuration)
    {
        await context.PlayConfigurations.AddAsync(configuration);
        return await SaveAsync();
    }

    public async Task<bool> UpdatePlayConfigurationAsync(PlayConfiguration configuration)
    {
        context.PlayConfigurations.Update(configuration);
        return await SaveAsync();
    }

    public async Task<bool> RemovePlayConfigurationAsync(Guid id)
    {
        context.PlayConfigurations.Remove(
            (await context.PlayConfigurations.FirstOrDefaultAsync(configuration => configuration.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountPlayConfigurationsAsync(
        Expression<Func<PlayConfiguration, bool>>? predicate = null)
    {
        var result = context.PlayConfigurations.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}