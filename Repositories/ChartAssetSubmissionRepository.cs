using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class ChartAssetSubmissionRepository(ApplicationDbContext context) : IChartAssetSubmissionRepository
{
    public async Task<ICollection<ChartAssetSubmission>> GetChartAssetSubmissionsAsync(List<string> order,
        List<bool> desc,
        int position, int take,
        Expression<Func<ChartAssetSubmission, bool>>? predicate = null)
    {
        var result = context.ChartAssetSubmissions.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ChartAssetSubmission> GetChartAssetSubmissionAsync(Guid id)
    {
        return (await context.ChartAssetSubmissions.FirstOrDefaultAsync(chartAssetSubmission =>
            chartAssetSubmission.Id == id))!;
    }

    public async Task<bool> ChartAssetSubmissionExistsAsync(Guid id)
    {
        return await context.ChartAssetSubmissions.AnyAsync(chartAssetSubmission => chartAssetSubmission.Id == id);
    }

    public async Task<bool> CreateChartAssetSubmissionAsync(ChartAssetSubmission chartAssetSubmission)
    {
        await context.ChartAssetSubmissions.AddAsync(chartAssetSubmission);
        return await SaveAsync();
    }

    public async Task<bool> UpdateChartAssetSubmissionAsync(ChartAssetSubmission chartAssetSubmission)
    {
        context.ChartAssetSubmissions.Update(chartAssetSubmission);
        return await SaveAsync();
    }

    public async Task<bool> RemoveChartAssetSubmissionAsync(Guid id)
    {
        context.ChartAssetSubmissions.Remove(
            (await context.ChartAssetSubmissions.FirstOrDefaultAsync(chartAssetSubmission =>
                chartAssetSubmission.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountChartAssetSubmissionsAsync(
        Expression<Func<ChartAssetSubmission, bool>>? predicate = null)
    {
        var result = context.ChartAssetSubmissions.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}