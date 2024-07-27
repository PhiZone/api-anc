using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class RecordRepository(ApplicationDbContext context, IMeilisearchService meilisearchService) : IRecordRepository
{
    public async Task<ICollection<Record>> GetRecordsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0, int? take = -1, Expression<Func<Record, bool>>? predicate = null, bool queryChart = false,
        int? currentUserId = null, bool? showAnonymous = false)
    {
        var result = context.Records
            .Where(e => (showAnonymous != null && showAnonymous.Value) ||
                        !(e.EventPresences.Any(f =>
                              f.Type == EventResourceType.Entry && f.IsAnonymous != null && f.IsAnonymous.Value) ||
                          e.Chart.EventPresences.Any(f =>
                              f.Type == EventResourceType.Entry && f.IsAnonymous != null && f.IsAnonymous.Value)))
            .Include(e => e.Owner)
            .ThenInclude(e => e.Region)
            .Include(e => e.EventPresences)
            .ThenInclude(e => e.Team)
            .OrderBy(order, desc);
        if (queryChart)
            result = result.Include(e => e.Chart)
                .ThenInclude(e => e.Song)
                .ThenInclude(e => e.Charts)
                .Include(e => e.Chart)
                .ThenInclude(e => e.Song)
                .ThenInclude(e => e.Tags)
                .Include(e => e.Chart)
                .ThenInclude(e => e.Tags)
                .Include(e => e.Chart)
                .ThenInclude(e => e.Song)
                .ThenInclude(e => e.EventPresences)
                .Include(e => e.Chart)
                .ThenInclude(e => e.EventPresences);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Record> GetRecordAsync(Guid id, bool queryChart = false, int? currentUserId = null)
    {
        IQueryable<Record> result = queryChart
            ? context.Records.Include(e => e.Chart)
                .ThenInclude(e => e.Song)
                .ThenInclude(e => e.Charts)
                .Include(e => e.Chart)
                .ThenInclude(e => e.Song)
                .ThenInclude(e => e.Tags)
                .Include(e => e.Chart)
                .ThenInclude(e => e.Tags)
                .Include(e => e.Owner)
                .ThenInclude(e => e.Region)
                .Include(e => e.EventPresences)
                .ThenInclude(e => e.Team)
                .Include(e => e.Chart)
                .ThenInclude(e => e.Song)
                .ThenInclude(e => e.EventPresences)
                .Include(e => e.Chart)
                .ThenInclude(e => e.EventPresences)
            : context.Records.Include(e => e.Owner)
                .ThenInclude(e => e.Region)
                .Include(e => e.EventPresences)
                .ThenInclude(e => e.Team);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        return (await result.FirstOrDefaultAsync(record => record.Id == id))!;
    }

    public async Task<bool> RecordExistsAsync(Guid id)
    {
        return await context.Records.AnyAsync(record => record.Id == id);
    }

    public async Task<bool> CreateRecordAsync(Record record)
    {
        var chart = await context.Charts.Include(e => e.Song)
            .ThenInclude(e => e.Tags)
            .FirstAsync(e => e.Id == record.ChartId);
        chart.PlayCount = await context.Records.LongCountAsync(e => e.ChartId == record.Chart.Id) + 1;
        chart.Song.PlayCount = await context.Records.LongCountAsync(e => e.Chart.SongId == record.Chart.SongId) + 1;
        await context.Records.AddAsync(record);
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
        await meilisearchService.UpdateAsync(record.Chart);
        await meilisearchService.UpdateAsync(record.Chart.Song);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountRecordsAsync(Expression<Func<Record, bool>>? predicate = null,
        bool? showAnonymous = false)
    {
        var result = context.Records.Where(e =>
            (showAnonymous != null && showAnonymous.Value) || !(e.EventPresences.Any(f =>
                                                                    f.Type == EventResourceType.Entry &&
                                                                    f.IsAnonymous != null && f.IsAnonymous.Value) ||
                                                                e.Chart.EventPresences.Any(f =>
                                                                    f.Type == EventResourceType.Entry &&
                                                                    f.IsAnonymous != null && f.IsAnonymous.Value)));

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }

    public async Task<ICollection<Record>> GetPersonalBests(int ownerId, int take = 19, bool queryChart = false,
        int? currentUserId = null)
    {
        IQueryable<Record> result = context.Records
            .Where(e => !e.EventPresences.Any(f =>
                f.Type == EventResourceType.Entry && f.IsAnonymous != null && f.IsAnonymous.Value))
            .Include(e => e.Owner)
            .ThenInclude(e => e.Region)
            .Include(e => e.EventPresences);
        if (queryChart)
            result = result.Include(e => e.Chart)
                .ThenInclude(e => e.Song)
                .ThenInclude(e => e.Charts)
                .Include(e => e.Chart)
                .ThenInclude(e => e.Song)
                .ThenInclude(e => e.Tags)
                .Include(e => e.Chart)
                .ThenInclude(e => e.Tags)
                .Include(e => e.Chart)
                .ThenInclude(e => e.Song)
                .ThenInclude(e => e.EventPresences)
                .Include(e => e.Chart)
                .ThenInclude(e => e.EventPresences);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        result = result.Where(e => e.Chart.IsRanked && e.OwnerId == ownerId)
            .GroupBy(e => e.ChartId)
            .Select(g => g.OrderByDescending(e => e.Rks).ThenBy(e => e.DateCreated).First());
        var list = (await result.ToListAsync()).OrderByDescending(e => e.Rks);
        return take >= 0 ? list.Take(take).ToList() : list.ToList();
    }
}