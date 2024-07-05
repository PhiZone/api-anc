using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IAdmissionRepository
{
    Task<ICollection<Admission>> GetAdmittersAsync(Guid admitteeId, List<string>? order = null, List<bool>? desc = null,
        int? position = 0,
        int? take = -1, Expression<Func<Admission, bool>>? predicate = null);

    Task<ICollection<Admission>> GetAdmitteesAsync(Guid admitterId, List<string>? order = null, List<bool>? desc = null,
        int? position = 0,
        int? take = -1, Expression<Func<Admission, bool>>? predicate = null);

    Task<ICollection<Admission>> GetAdmissionsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0, int? take = -1,
        Expression<Func<Admission, bool>>? predicate = null);

    Task<Admission> GetAdmissionAsync(Guid admitterId, Guid admitteeId);

    Task<bool> CreateAdmissionAsync(Admission admission);

    Task<bool> UpdateAdmissionAsync(Admission admission);

    Task<bool> RemoveAdmissionAsync(Guid admitterId, Guid admitteeId);

    Task<bool> SaveAsync();

    Task<int> CountAdmissionsAsync(Expression<Func<Admission, bool>>? predicate = null);

    Task<bool> AdmissionExistsAsync(Guid admitterId, Guid admitteeId);

    Task<int> CountAdmittersAsync(Guid admitteeId, Expression<Func<Admission, bool>>? predicate = null);

    Task<int> CountAdmitteesAsync(Guid admitterId, Expression<Func<Admission, bool>>? predicate = null);
}