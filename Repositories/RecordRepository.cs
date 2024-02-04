using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;
using Z.EntityFramework.Plus;

namespace PhiZoneApi.Repositories;

public class RecordRepository(ApplicationDbContext context, IMeilisearchService meilisearchService) : IRecordRepository
{
    public async Task<ICollection<Record>> GetRecordsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Record, bool>>? predicate = null, bool queryChart = false, int? currentUserId = null)
    {
        var result = context.Records.Include(e => e.Owner).ThenInclude(e => e.Region).OrderBy(order, desc);
        if (queryChart)
            result = result.Include(e => e.Chart)
                .ThenInclude(e => e.Song)
                .ThenInclude(e => e.Tags)
                .Include(e => e.Chart)
                .ThenInclude(e => e.Tags);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.IncludeFilter(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Record> GetRecordAsync(Guid id, bool queryChart = false, int? currentUserId = null)
    {
        IQueryable<Record> result = queryChart
            ? context.Records.Include(e => e.Chart)
                .ThenInclude(e => e.Song)
                .ThenInclude(e => e.Tags)
                .Include(e => e.Chart)
                .ThenInclude(e => e.Tags)
                .Include(e => e.Owner)
                .ThenInclude(e => e.Region)
            : context.Records.Include(e => e.Owner).ThenInclude(e => e.Region);
        if (currentUserId != null)
            result = result.IncludeFilter(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        return (await result.FirstOrDefaultAsync(record => record.Id == id))!;
    }

    public async Task<bool> RecordExistsAsync(Guid id)
    {
        return await context.Records.AnyAsync(record => record.Id == id);
    }

    public async Task<bool> CreateRecordAsync(Record record)
    {
        var chart = await context.Charts.Include(chart => chart.Song).FirstAsync(e => e.Id == record.ChartId);
        chart.PlayCount = await context.Records.CountAsync(e => e.ChartId == record.Chart.Id) + 1;
        chart.Song.PlayCount = await context.Records.CountAsync(e => e.Chart.SongId == record.Chart.SongId) + 1;
        await context.Records.AddAsync(record);
        context.Charts.Update(record.Chart);
        context.Songs.Update(record.Chart.Song);
        await meilisearchService.UpdateAsync(record.Chart);
        await meilisearchService.UpdateAsync(record.Chart.Song);
        return await SaveAsync();
    }

    public async Task<bool> UpdateRecordAsync(Record record)
    {
        context.Records.Update(record);
        return await SaveAsync();
    }

    public async Task<bool> UpdateRecordsAsync(IEnumerable<Record> records)
    {
        context.Records.UpdateRange(records);
        return await SaveAsync();
    }

    public async Task<bool> RemoveRecordAsync(Guid id)
    {
        var record = await context.Records.Include(e => e.Chart)
            .ThenInclude(e => e.Song)
            .ThenInclude(e => e.Tags)
            .Include(e => e.Chart)
            .ThenInclude(e => e.Tags)
            .FirstAsync(record => record.Id == id);
        record.Chart.PlayCount = await context.Records.CountAsync(e => e.ChartId == record.Chart.Id) - 1;
        record.Chart.Song.PlayCount = await context.Records.CountAsync(e => e.Chart.SongId == record.Chart.SongId) - 1;
        context.Records.Remove(record);
        context.Charts.Update(record.Chart);
        context.Songs.Update(record.Chart.Song);
        await meilisearchService.UpdateAsync(record.Chart);
        await meilisearchService.UpdateAsync(record.Chart.Song);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountRecordsAsync(Expression<Func<Record, bool>>? predicate = null)
    {
        var result = context.Records.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }

    public async Task<ICollection<Record>> GetPersonalBests(int ownerId, int take = 19, bool queryChart = false,
        int? currentUserId = null)
    {
        IQueryable<Record> result = context.Records.Include(e => e.Owner).ThenInclude(e => e.Region);
        if (queryChart)
            result = result.Include(e => e.Chart)
                .ThenInclude(e => e.Song)
                .ThenInclude(e => e.Tags)
                .Include(e => e.Chart)
                .ThenInclude(e => e.Tags);
        result = result.Where(e => e.Chart.IsRanked && e.OwnerId == ownerId)
            .GroupBy(e => e.ChartId)
            .Select(g => g.OrderByDescending(e => e.Rks).ThenBy(e => e.DateCreated).First());
        if (currentUserId != null)
            result = result.IncludeFilter(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        var list = (await result.ToListAsync()).OrderByDescending(e => e.Rks);
        return take >= 0 ? list.Take(take).ToList() : list.ToList();
    }
}