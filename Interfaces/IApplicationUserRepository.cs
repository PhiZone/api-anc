using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IApplicationUserRepository
{
    Task<ICollection<ApplicationUser>> GetApplicationsAsync(int userId, List<string> order, List<bool> desc,
        int position,
        int take, Expression<Func<ApplicationUser, bool>>? predicate = null);

    Task<ICollection<ApplicationUser>> GetUsersAsync(Guid applicationId, List<string> order, List<bool> desc,
        int position,
        int take, Expression<Func<ApplicationUser, bool>>? predicate = null);

    Task<ICollection<ApplicationUser>> GetRelationsAsync(List<string> order, List<bool> desc, int position,
        int take,
        Expression<Func<ApplicationUser, bool>>? predicate = null);

    Task<ApplicationUser> GetRelationAsync(Guid applicationId, int userId);

    Task<ApplicationUser> GetRelationAsync(Guid applicationId, string remoteUserId);

    Task<bool> CreateRelationAsync(ApplicationUser applicationUser);

    Task<bool> UpdateRelationAsync(ApplicationUser applicationUser);

    Task<bool> RemoveRelationAsync(Guid applicationId, int userId);

    Task<bool> SaveAsync();

    Task<int> CountRelationsAsync(Expression<Func<ApplicationUser, bool>>? predicate = null);

    Task<bool> RelationExistsAsync(Guid applicationId, int userId);

    Task<bool> RelationExistsAsync(Guid applicationId, string remoteUserId);

    Task<int> CountApplicationsAsync(int userId, Expression<Func<ApplicationUser, bool>>? predicate = null);

    Task<int> CountUsersAsync(Guid applicationId, Expression<Func<ApplicationUser, bool>>? predicate = null);
}