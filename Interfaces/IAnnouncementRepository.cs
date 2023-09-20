using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IAnnouncementRepository
{
    Task<ICollection<Announcement>> GetAnnouncementsAsync(List<string> order, List<bool> desc, int position, int take,
        string? search = null, Expression<Func<Announcement, bool>>? predicate = null);

    Task<Announcement> GetAnnouncementAsync(Guid id);

    Task<bool> AnnouncementExistsAsync(Guid id);

    Task<bool> CreateAnnouncementAsync(Announcement announcement);

    Task<bool> UpdateAnnouncementAsync(Announcement announcement);

    Task<bool> RemoveAnnouncementAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountAnnouncementsAsync(string? search = null, Expression<Func<Announcement, bool>>? predicate = null);
}