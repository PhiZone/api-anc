using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface ITapUserRelationRepository
{
    Task<ICollection<TapUserRelation>> GetApplicationsAsync(int userId, List<string> order, List<bool> desc,
        int position,
        int take, Expression<Func<TapUserRelation, bool>>? predicate = null);

    Task<ICollection<TapUserRelation>> GetUsersAsync(Guid applicationId, List<string> order, List<bool> desc,
        int position,
        int take, Expression<Func<TapUserRelation, bool>>? predicate = null);

    Task<ICollection<TapUserRelation>> GetRelationsAsync(List<string> order, List<bool> desc, int position,
        int take,
        Expression<Func<TapUserRelation, bool>>? predicate = null);

    Task<TapUserRelation> GetRelationAsync(Guid applicationId, int userId);

    Task<bool> CreateRelationAsync(TapUserRelation userRelation);

    Task<bool> UpdateRelationAsync(TapUserRelation userRelation);

    Task<bool> RemoveRelationAsync(Guid applicationId, int userId);

    Task<bool> SaveAsync();

    Task<int> CountRelationsAsync(Expression<Func<TapUserRelation, bool>>? predicate = null);

    Task<bool> RelationExistsAsync(Guid applicationId, int userId);

    Task<int> CountApplicationsAsync(int userId, Expression<Func<TapUserRelation, bool>>? predicate = null);

    Task<int> CountUsersAsync(Guid applicationId, Expression<Func<TapUserRelation, bool>>? predicate = null);
}