using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;
using Z.EntityFramework.Plus;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class ChartRepository(ApplicationDbContext context, IMeilisearchService meilisearchService) : IChartRepository
{
    public async Task<ICollection<Chart>> GetChartsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Chart, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.Charts.Include(e => e.Song).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.IncludeFilter(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Chart> GetChartAsync(Guid id, int? currentUserId = null)
    {
        IQueryable<Chart> result = context.Charts.Include(e => e.Song);
        if (currentUserId != null)
            result = result.IncludeFilter(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        return (await result.FirstOrDefaultAsync(chart => chart.Id == id))!;
    }

    public async Task<Chart?> GetRandomChartAsync(
        Expression<Func<Chart, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.Charts.Include(e => e.Song).OrderBy(chart => EF.Functions.Random()).AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.IncludeFilter(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        return await result.FirstOrDefaultAsync();
    }

    public async Task<ICollection<Record>> GetChartRecordsAsync(Guid id, List<string> order, List<bool> desc,
        int position,
        int take, Expression<Func<Record, bool>>? predicate = null, int? currentUserId = null)
    {
        var chart = (await context.Charts.FirstOrDefaultAsync(chart => chart.Id == id))!;
        var result = context.Records.Where(record => record.Chart.Id == chart.Id).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position).Include(e => e.Chart).ThenInclude(e => e.Song);
        if (currentUserId != null)
            result = result.IncludeFilter(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<bool> ChartExistsAsync(Guid id)
    {
        return (await context.Charts.AnyAsync(chart => chart.Id == id))!;
    }

    public async Task<bool> CreateChartAsync(Chart chart)
    {
        await context.Charts.AddAsync(chart);
        await meilisearchService.AddAsync(chart);
        return await SaveAsync();
    }

    public async Task<bool> UpdateChartAsync(Chart chart)
    {
        context.Charts.Update(chart);
        await meilisearchService.UpdateAsync(chart);
        return await SaveAsync();
    }

    public async Task<bool> UpdateChartsAsync(IEnumerable<Chart> charts)
    {
        var enumerable = charts.ToList();
        context.Charts.UpdateRange(enumerable);
        await meilisearchService.UpdateBatchAsync(enumerable);
        return await SaveAsync();
    }

    public async Task<bool> RemoveChartAsync(Guid id)
    {
        context.Charts.Remove((await context.Charts.FirstOrDefaultAsync(chart => chart.Id == id))!);
        await meilisearchService.DeleteAsync<Chart>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountChartsAsync(Expression<Func<Chart, bool>>? predicate = null)
    {
        var result = context.Charts.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }

    public async Task<int> CountChartRecordsAsync(Guid id,
        Expression<Func<Record, bool>>? predicate = null)
    {
        var chart = (await context.Charts.FirstOrDefaultAsync(chart => chart.Id == id))!;
        var result = context.Records.Where(record => record.Chart.Id == chart.Id);
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}