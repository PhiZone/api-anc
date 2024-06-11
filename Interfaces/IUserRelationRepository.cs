using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IUserRelationRepository
{
    Task<ICollection<UserRelation>> GetFollowersAsync(int userId, List<string>? order = null, List<bool>? desc = null,
        int? position = 0,
        int? take = -1,
        Expression<Func<UserRelation, bool>>? predicate = null, int? currentUserId = null);

    Task<ICollection<UserRelation>> GetFolloweesAsync(int userId, List<string>? order = null, List<bool>? desc = null,
        int? position = 0,
        int? take = -1,
        Expression<Func<UserRelation, bool>>? predicate = null, int? currentUserId = null);

    Task<ICollection<UserRelation>> GetRelationsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0, int? take = -1,
        Expression<Func<UserRelation, bool>>? predicate = null);

    Task<UserRelation> GetRelationAsync(int followerId, int followeeId);

    Task<bool> CreateRelationAsync(UserRelation userRelation);

    Task<bool> UpdateRelationAsync(UserRelation userRelation);

    Task<bool> RemoveRelationAsync(int followerId, int followeeId);

    Task<bool> SaveAsync();

    Task<int> CountRelationsAsync(Expression<Func<UserRelation, bool>>? predicate = null);

    Task<bool> RelationExistsAsync(int followerId, int followeeId);

    Task<int> CountFollowersAsync(int userId, Expression<Func<UserRelation, bool>>? predicate = null);

    Task<int> CountFolloweesAsync(int userId, Expression<Func<UserRelation, bool>>? predicate = null);
}