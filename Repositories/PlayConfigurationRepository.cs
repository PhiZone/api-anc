using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class PlayConfigurationRepository : IPlayConfigurationRepository
{
    private readonly ApplicationDbContext _context;

    public PlayConfigurationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<PlayConfiguration>> GetPlayConfigurationsAsync(List<string> order, List<bool> desc, int position,
        int take,
        string? search = null, Expression<Func<PlayConfiguration, bool>>? predicate = null)
    {
        var result = _context.PlayConfigurations.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<PlayConfiguration> GetPlayConfigurationAsync(Guid id)
    {
        return (await _context.PlayConfigurations.FirstOrDefaultAsync(configuration => configuration.Id == id))!;
    }

    public async Task<bool> PlayConfigurationExistsAsync(Guid id)
    {
        return await _context.PlayConfigurations.AnyAsync(configuration => configuration.Id == id);
    }

    public async Task<bool> CreatePlayConfigurationAsync(PlayConfiguration configuration)
    {
        await _context.PlayConfigurations.AddAsync(configuration);
        return await SaveAsync();
    }

    public async Task<bool> UpdatePlayConfigurationAsync(PlayConfiguration configuration)
    {
        _context.PlayConfigurations.Update(configuration);
        return await SaveAsync();
    }

    public async Task<bool> RemovePlayConfigurationAsync(Guid id)
    {
        _context.PlayConfigurations.Remove(
            (await _context.PlayConfigurations.FirstOrDefaultAsync(configuration => configuration.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountPlayConfigurationsAsync(string? search = null,
        Expression<Func<PlayConfiguration, bool>>? predicate = null)
    {
        var result = _context.PlayConfigurations.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}