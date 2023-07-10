using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly ApplicationDbContext _context;

    public ApplicationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Application>> GetApplicationsAsync(string order, bool desc, int position, int take,
        string? search = null, Expression<Func<Application, bool>>? predicate = null)
    {
        var result = _context.Applications.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<Application> GetApplicationAsync(Guid id)
    {
        return (await _context.Applications.FirstOrDefaultAsync(application => application.Id == id))!;
    }

    public async Task<bool> ApplicationExistsAsync(Guid id)
    {
        return await _context.Applications.AnyAsync(application => application.Id == id);
    }

    public async Task<bool> CreateApplicationAsync(Application application)
    {
        await _context.Applications.AddAsync(application);
        return await SaveAsync();
    }

    public async Task<bool> UpdateApplicationAsync(Application application)
    {
        _context.Applications.Update(application);
        return await SaveAsync();
    }

    public async Task<bool> RemoveApplicationAsync(Guid id)
    {
        _context.Applications.Remove(
            (await _context.Applications.FirstOrDefaultAsync(application => application.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountApplicationsAsync(string? search = null,
        Expression<Func<Application, bool>>? predicate = null)
    {
        var result = _context.Applications.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}