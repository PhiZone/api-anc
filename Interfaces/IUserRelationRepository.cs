using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IUserRelationRepository
{
    Task<ICollection<UserRelation>> GetFollowersAsync(int userId, string order, bool desc, int position, int take);

    Task<ICollection<UserRelation>> GetFolloweesAsync(int userId, string order, bool desc, int position, int take);

    Task<ICollection<UserRelation>> GetRelationsAsync(string order, bool desc, int position, int take);

    Task<UserRelation> GetRelationAsync(int followerId, int followeeId);

    Task<bool> CreateRelationAsync(UserRelation userRelation);

    Task<bool> RemoveRelationAsync(UserRelation userRelation);

    Task<bool> SaveAsync();

    Task<int> CountAsync();

    Task<bool> RelationExistsAsync(int followerId, int followeeId);

    Task<int> CountFollowersAsync(User user);

    Task<int> CountFolloweesAsync(User user);
}