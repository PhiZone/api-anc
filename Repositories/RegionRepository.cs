using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class RegionRepository : IRegionRepository
{
    private readonly ApplicationDbContext _context;

    public RegionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Region>> GetRegionsAsync(string order, bool desc, int position, int take,
        string? search = null, Expression<Func<Region, bool>>? predicate = null)
    {
        var result = _context.Regions.OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(region =>
                region.Code.ToUpper().Contains(search) || region.Name.ToUpper().Contains(search));
        }

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<Region> GetRegionAsync(int id)
    {
        return (await _context.Regions.FirstOrDefaultAsync(region => region.Id == id))!;
    }

    public async Task<Region> GetRegionAsync(string code)
    {
        return (await _context.Regions.FirstOrDefaultAsync(region => string.Equals(region.Code, code.ToUpper())))!;
    }

    public async Task<ICollection<User>> GetRegionUsersAsync(int id, string order, bool desc, int position,
        int take, string? search = null, Expression<Func<User, bool>>? predicate = null)
    {
        var region = (await _context.Regions.FirstOrDefaultAsync(region => region.Id == id))!;
        var result = _context.Users.Where(user => user.Region == region).OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(user =>
                (user.NormalizedUserName != null && user.NormalizedUserName.Contains(search)) ||
                (user.Tag != null && user.Tag.ToUpper().Contains(search)) ||
                (user.Biography != null && user.Biography.ToUpper().Contains(search)) ||
                user.Language.ToUpper().Contains(search));
        }

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<ICollection<User>> GetRegionUsersAsync(string code, string order, bool desc, int position,
        int take, string? search = null, Expression<Func<User, bool>>? predicate = null)
    {
        var region = (await _context.Regions.FirstOrDefaultAsync(region =>
            string.Equals(region.Code, code.ToUpper())))!;
        var result = _context.Users.Where(user => user.Region == region).OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(user =>
                (user.NormalizedUserName != null && user.NormalizedUserName.Contains(search)) ||
                (user.Tag != null && user.Tag.ToUpper().Contains(search)) ||
                (user.Biography != null && user.Biography.ToUpper().Contains(search)) ||
                user.Language.ToUpper().Contains(search));
        }

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<bool> RegionExistsAsync(int id)
    {
        return await _context.Regions.AnyAsync(region => region.Id == id);
    }

    public async Task<bool> RegionExistsAsync(string code)
    {
        return await _context.Regions.AnyAsync(region => string.Equals(region.Code, code.ToUpper()));
    }

    public bool RegionExists(string code)
    {
        return _context.Regions.Any(region => string.Equals(region.Code, code.ToUpper()));
    }

    public async Task<bool> CreateRegionAsync(Region region)
    {
        await _context.AddAsync(region);
        return await SaveAsync();
    }

    public async Task<bool> UpdateRegionAsync(Region region)
    {
        _context.Update(region);
        return await SaveAsync();
    }

    public async Task<bool> RemoveRegionAsync(string code)
    {
        _context.Regions.Remove((await _context.Regions.FirstOrDefaultAsync(region => region.Code == code.ToUpper()))!);
        return await SaveAsync();
    }

    public async Task<bool> RemoveRegionAsync(int id)
    {
        _context.Regions.Remove((await _context.Regions.FirstOrDefaultAsync(region => region.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountAsync(string? search = null, Expression<Func<Region, bool>>? predicate = null)
    {
        var result = _context.Regions.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(region =>
                region.Code.ToUpper().Contains(search) || region.Name.ToUpper().Contains(search));
        }

        return await result.CountAsync();
    }

    public async Task<int> CountUsersAsync(string code, string? search = null,
        Expression<Func<User, bool>>? predicate = null)
    {
        var region = (await _context.Regions.FirstOrDefaultAsync(region =>
            string.Equals(region.Code, code.ToUpper())))!;
        var result = _context.Users.Where(user => user.Region == region);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(user =>
                (user.NormalizedUserName != null && user.NormalizedUserName.Contains(search)) ||
                (user.Tag != null && user.Tag.ToUpper().Contains(search)) ||
                (user.Biography != null && user.Biography.ToUpper().Contains(search)) ||
                user.Language.ToUpper().Contains(search));
        }

        return await result.CountAsync();
    }

    public async Task<int> CountUsersAsync(int id, string? search = null,
        Expression<Func<User, bool>>? predicate = null)
    {
        var region = (await _context.Regions.FirstOrDefaultAsync(region => region.Id == id))!;
        var result = _context.Users.Where(user => user.Region == region);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(user =>
                (user.NormalizedUserName != null && user.NormalizedUserName.Contains(search)) ||
                (user.Tag != null && user.Tag.ToUpper().Contains(search)) ||
                (user.Biography != null && user.Biography.ToUpper().Contains(search)) ||
                user.Language.ToUpper().Contains(search));
        }

        return await result.CountAsync();
    }
}