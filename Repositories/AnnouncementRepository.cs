using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class AnnouncementRepository(ApplicationDbContext context, IMeilisearchService meilisearchService)
    : IAnnouncementRepository
{
    public async Task<ICollection<Announcement>> GetAnnouncementsAsync(List<string> order, List<bool> desc,
        int position, int take, Expression<Func<Announcement, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.Announcements.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Announcement> GetAnnouncementAsync(Guid id, int? currentUserId = null)
    {
        IQueryable<Announcement> result = context.Announcements;
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        return (await result.FirstOrDefaultAsync(announcement => announcement.Id == id))!;
    }

    public async Task<bool> AnnouncementExistsAsync(Guid id)
    {
        return await context.Announcements.AnyAsync(announcement => announcement.Id == id);
    }

    public async Task<bool> CreateAnnouncementAsync(Announcement announcement)
    {
        await context.Announcements.AddAsync(announcement);
        await meilisearchService.AddAsync(announcement);
        return await SaveAsync();
    }

    public async Task<bool> UpdateAnnouncementAsync(Announcement announcement)
    {
        context.Announcements.Update(announcement);
        await meilisearchService.UpdateAsync(announcement);
        return await SaveAsync();
    }

    public async Task<bool> RemoveAnnouncementAsync(Guid id)
    {
        context.Announcements.Remove(
            (await context.Announcements.FirstOrDefaultAsync(announcement => announcement.Id == id))!);
        await meilisearchService.DeleteAsync<Announcement>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountAnnouncementsAsync(Expression<Func<Announcement, bool>>? predicate = null)
    {
        var result = context.Announcements.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}