using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class ChartAssetRepository : IChartAssetRepository
{
    private readonly ApplicationDbContext _context;

    public ChartAssetRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<ChartAsset>> GetChartAssetsAsync(List<string> order, List<bool> desc, int position,
        int take,
        Expression<Func<ChartAsset, bool>>? predicate = null)
    {
        var result = _context.ChartAssets.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ChartAsset> GetChartAssetAsync(Guid id)
    {
        return (await _context.ChartAssets.FirstOrDefaultAsync(chartAsset => chartAsset.Id == id))!;
    }

    public async Task<bool> ChartAssetExistsAsync(Guid id)
    {
        return await _context.ChartAssets.AnyAsync(chartAsset => chartAsset.Id == id);
    }

    public async Task<bool> CreateChartAssetAsync(ChartAsset chartAsset)
    {
        await _context.ChartAssets.AddAsync(chartAsset);
        return await SaveAsync();
    }

    public async Task<bool> UpdateChartAssetAsync(ChartAsset chartAsset)
    {
        _context.ChartAssets.Update(chartAsset);
        return await SaveAsync();
    }

    public async Task<bool> RemoveChartAssetAsync(Guid id)
    {
        _context.ChartAssets.Remove(
            (await _context.ChartAssets.FirstOrDefaultAsync(chartAsset => chartAsset.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountChartAssetsAsync(Expression<Func<ChartAsset, bool>>? predicate = null)
    {
        var result = _context.ChartAssets.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}