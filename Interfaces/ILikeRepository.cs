using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface ILikeRepository
{
    Task<ICollection<Like>> GetLikesAsync(string order, bool desc, int position, int take,
        Expression<Func<Like, bool>>? predicate = null);

    Task<Like> GetLikeAsync(Guid id);

    Task<Like> GetLikeAsync(Guid resourceId, int userId);

    Task<bool> LikeExistsAsync(Guid id);

    Task<bool> LikeExistsAsync(Guid resourceId, int userId);

    Task<bool> CreateLikeAsync(Like like);

    Task<bool> RemoveLikeAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountAsync(Expression<Func<Like, bool>>? predicate = null);
}