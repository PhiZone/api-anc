using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IAnnouncementRepository
{
    Task<ICollection<Announcement>> GetAnnouncementsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Announcement, bool>>? predicate = null, int? currentUserId = null);

    Task<Announcement> GetAnnouncementAsync(Guid id, int? currentUserId = null);

    Task<bool> AnnouncementExistsAsync(Guid id);

    Task<bool> CreateAnnouncementAsync(Announcement announcement);

    Task<bool> UpdateAnnouncementAsync(Announcement announcement);

    Task<bool> RemoveAnnouncementAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountAnnouncementsAsync(Expression<Func<Announcement, bool>>? predicate = null);
}