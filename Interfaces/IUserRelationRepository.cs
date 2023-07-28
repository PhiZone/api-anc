using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IUserRelationRepository
{
    Task<ICollection<UserRelation>> GetFollowersAsync(int userId, string order, bool desc, int position, int take,
        Expression<Func<UserRelation, bool>>? predicate = null);

    Task<ICollection<UserRelation>> GetFolloweesAsync(int userId, string order, bool desc, int position, int take,
        Expression<Func<UserRelation, bool>>? predicate = null);

    Task<ICollection<UserRelation>> GetRelationsAsync(string order, bool desc, int position, int take,
        Expression<Func<UserRelation, bool>>? predicate = null);

    Task<UserRelation> GetRelationAsync(int followerId, int followeeId);

    Task<bool> CreateRelationAsync(UserRelation userRelation);

    Task<bool> RemoveRelationAsync(int followerId, int followeeId);

    Task<bool> SaveAsync();

    Task<int> CountRelationsAsync(Expression<Func<UserRelation, bool>>? predicate = null);

    Task<bool> RelationExistsAsync(int followerId, int followeeId);

    Task<int> CountFollowersAsync(int userId, Expression<Func<UserRelation, bool>>? predicate = null);

    Task<int> CountFolloweesAsync(int userId, Expression<Func<UserRelation, bool>>? predicate = null);
}