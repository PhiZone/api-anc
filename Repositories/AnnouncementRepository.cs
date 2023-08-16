using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly ApplicationDbContext _context;

    public AnnouncementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Announcement>> GetAnnouncementsAsync(string order, bool desc, int position, int take,
        string? search = null, Expression<Func<Announcement, bool>>? predicate = null)
    {
        var result = _context.Announcements.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(announcement => announcement.Title.ToUpper().Like(search) ||
                                                  announcement.Content.ToUpper().Like(search));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Announcement> GetAnnouncementAsync(Guid id)
    {
        return (await _context.Announcements.FirstOrDefaultAsync(announcement => announcement.Id == id))!;
    }

    public async Task<bool> AnnouncementExistsAsync(Guid id)
    {
        return await _context.Announcements.AnyAsync(announcement => announcement.Id == id);
    }

    public async Task<bool> CreateAnnouncementAsync(Announcement announcement)
    {
        await _context.Announcements.AddAsync(announcement);
        return await SaveAsync();
    }

    public async Task<bool> UpdateAnnouncementAsync(Announcement announcement)
    {
        _context.Announcements.Update(announcement);
        return await SaveAsync();
    }

    public async Task<bool> RemoveAnnouncementAsync(Guid id)
    {
        _context.Announcements.Remove(
            (await _context.Announcements.FirstOrDefaultAsync(announcement => announcement.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountAnnouncementsAsync(string? search = null,
        Expression<Func<Announcement, bool>>? predicate = null)
    {
        var result = _context.Announcements.AsQueryable();

        if (predicate != null) result = result.Where(predicate);
        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(announcement => announcement.Title.ToUpper().Like(search) ||
                                                  announcement.Content.ToUpper().Like(search));
        }

        return await result.CountAsync();
    }
}