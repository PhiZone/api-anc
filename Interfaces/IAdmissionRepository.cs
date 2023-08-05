using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IAdmissionRepository
{
    Task<ICollection<Admission>> GetAdmittersAsync(Guid admitteeId, string order, bool desc, int position,
        int take, Expression<Func<Admission, bool>>? predicate = null);

    Task<ICollection<Admission>> GetAdmitteesAsync(Guid admitterId, string order, bool desc, int position,
        int take, Expression<Func<Admission, bool>>? predicate = null);

    Task<ICollection<Admission>> GetAdmissionsAsync(string order, bool desc, int position, int take,
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