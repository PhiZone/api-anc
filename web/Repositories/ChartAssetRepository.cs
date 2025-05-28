using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class ChartAssetRepository(ApplicationDbContext context) : IChartAssetRepository
{
    public async Task<ICollection<ChartAsset>> GetChartAssetsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0,
        int? take = -1,
        Expression<Func<ChartAsset, bool>>? predicate = null)
    {
        var result = context.ChartAssets.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ChartAsset> GetChartAssetAsync(Guid id)
    {
        return (await context.ChartAssets.FirstOrDefaultAsync(chartAsset => chartAsset.Id == id))!;
    }

    public async Task<bool> ChartAssetExistsAsync(Guid id)
    {
        return await context.ChartAssets.AnyAsync(chartAsset => chartAsset.Id == id);
    }

    public async Task<bool> CreateChartAssetAsync(ChartAsset chartAsset)
    {
        await context.ChartAssets.AddAsync(chartAsset);
        return await SaveAsync();
    }

    public async Task<bool> UpdateChartAssetAsync(ChartAsset chartAsset)
    {
        context.ChartAssets.Update(chartAsset);
        return await SaveAsync();
    }

    public async Task<bool> RemoveChartAssetAsync(Guid id)
    {
        context.ChartAssets.Remove(
            (await context.ChartAssets.FirstOrDefaultAsync(chartAsset => chartAsset.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountChartAssetsAsync(Expression<Func<ChartAsset, bool>>? predicate = null)
    {
        var result = context.ChartAssets.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}