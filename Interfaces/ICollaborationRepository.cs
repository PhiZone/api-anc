using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface ICollaborationRepository
{
    Task<ICollection<Collaboration>> GetCollaborationsAsync(string order, bool desc, int position, int take,
        Expression<Func<Collaboration, bool>>? predicate = null);

    Task<Collaboration> GetCollaborationAsync(Guid id);

    Task<Collaboration> GetCollaborationAsync(Guid submissionId, int inviteeId);

    Task<bool> CreateCollaborationAsync(Collaboration collaboration);

    Task<bool> UpdateCollaborationAsync(Collaboration collaboration);

    Task<bool> RemoveCollaborationAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountCollaborationsAsync(Expression<Func<Collaboration, bool>>? predicate = null);

    Task<bool> CollaborationExistsAsync(Guid id);

    Task<bool> CollaborationExistsAsync(Guid submissionId, int inviteeId);
}