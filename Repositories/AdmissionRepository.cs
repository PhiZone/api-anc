using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class AdmissionRepository : IAdmissionRepository
{
    private readonly ApplicationDbContext _context;

    public AdmissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Admission>> GetAdmittersAsync(Guid admitteeId, string order, bool desc, int position,
        int take, Expression<Func<Admission, bool>>? predicate = null)
    {
        var result = _context.Admissions.Where(admission => admission.AdmitteeId == admitteeId).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<Admission>> GetAdmitteesAsync(Guid admitterId, string order, bool desc, int position,
        int take, Expression<Func<Admission, bool>>? predicate = null)
    {
        var result = _context.Admissions.Where(admission => admission.AdmitterId == admitterId).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<Admission>> GetAdmissionsAsync(string order, bool desc, int position, int take,
        Expression<Func<Admission, bool>>? predicate = null)
    {
        var result = _context.Admissions.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Admission> GetAdmissionAsync(Guid admitterId, Guid admitteeId)
    {
        return (await _context.Admissions.FirstOrDefaultAsync(admission =>
            admission.AdmitterId == admitterId && admission.AdmitteeId == admitteeId))!;
    }

    public async Task<bool> CreateAdmissionAsync(Admission admission)
    {
        await _context.Admissions.AddAsync(admission);
        return await SaveAsync();
    }

    public async Task<bool> UpdateAdmissionAsync(Admission admission)
    {
        _context.Admissions.Update(admission);
        return await SaveAsync();
    }

    public async Task<bool> RemoveAdmissionAsync(Guid admitterId, Guid admitteeId)
    {
        _context.Admissions.Remove((await _context.Admissions.FirstOrDefaultAsync(admission =>
            admission.AdmitterId == admitterId && admission.AdmitteeId == admitteeId))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountAdmissionsAsync(Expression<Func<Admission, bool>>? predicate = null)
    {
        if (predicate != null) return await _context.Admissions.Where(predicate).CountAsync();
        return await _context.Admissions.CountAsync();
    }

    public async Task<bool> AdmissionExistsAsync(Guid admitterId, Guid admitteeId)
    {
        return await _context.Admissions.AnyAsync(admission =>
            admission.AdmitterId == admitterId && admission.AdmitteeId == admitteeId);
    }

    public async Task<int> CountAdmittersAsync(Guid admitteeId, Expression<Func<Admission, bool>>? predicate = null)
    {
        if (predicate != null)
            return await _context.Admissions.Where(admission => admission.Admittee.Id == admitteeId)
                .Where(predicate)
                .CountAsync();

        return await _context.Admissions.Where(admission => admission.Admittee.Id == admitteeId).CountAsync();
    }

    public async Task<int> CountAdmitteesAsync(Guid admitterId, Expression<Func<Admission, bool>>? predicate = null)
    {
        if (predicate != null)
            return await _context.Admissions.Where(admission => admission.Admitter.Id == admitterId)
                .Where(predicate)
                .CountAsync();

        return await _context.Admissions.Where(admission => admission.Admitter.Id == admitterId).CountAsync();
    }
}