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
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(region =>
                EF.Functions.Like(region.Code.ToUpper(), search) || EF.Functions.Like(region.Name.ToUpper(), search));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
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
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(user =>
                (user.NormalizedUserName != null && EF.Functions.Like(user.NormalizedUserName, search)) ||
                (user.Tag != null && EF.Functions.Like(user.Tag.ToUpper(), search)) ||
                (user.Biography != null && EF.Functions.Like(user.Biography.ToUpper(), search)) ||
            EF.Functions.Like(user.Language.ToUpper(), search));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
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
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(user =>
                (user.NormalizedUserName != null && EF.Functions.Like(user.NormalizedUserName, search)) ||
                (user.Tag != null && EF.Functions.Like(user.Tag.ToUpper(), search)) ||
                (user.Biography != null && EF.Functions.Like(user.Biography.ToUpper(), search)) ||
                EF.Functions.Like(user.Language.ToUpper(), search));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
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
        await _context.Regions.AddAsync(region);
        return await SaveAsync();
    }

    public async Task<bool> UpdateRegionAsync(Region region)
    {
        _context.Regions.Update(region);
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

    public async Task<int> CountRegionsAsync(string? search = null, Expression<Func<Region, bool>>? predicate = null)
    {
        var result = _context.Regions.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(region =>
                EF.Functions.Like(region.Code.ToUpper(), search) || EF.Functions.Like(region.Name.ToUpper(), search));
        }

        return await result.CountAsync();
    }

    public async Task<int> CountRegionUsersAsync(string code, string? search = null,
        Expression<Func<User, bool>>? predicate = null)
    {
        var region = (await _context.Regions.FirstOrDefaultAsync(region =>
            string.Equals(region.Code, code.ToUpper())))!;
        var result = _context.Users.Where(user => user.Region == region);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(user =>
                (user.NormalizedUserName != null && EF.Functions.Like(user.NormalizedUserName, search)) ||
                (user.Tag != null && EF.Functions.Like(user.Tag.ToUpper(), search)) ||
                (user.Biography != null && EF.Functions.Like(user.Biography.ToUpper(), search)) ||
                EF.Functions.Like(user.Language.ToUpper(), search));
        }

        return await result.CountAsync();
    }

    public async Task<int> CountRegionUsersAsync(int id, string? search = null,
        Expression<Func<User, bool>>? predicate = null)
    {
        var region = (await _context.Regions.FirstOrDefaultAsync(region => region.Id == id))!;
        var result = _context.Users.Where(user => user.Region == region);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(user =>
                (user.NormalizedUserName != null && EF.Functions.Like(user.NormalizedUserName, search)) ||
                (user.Tag != null && EF.Functions.Like(user.Tag.ToUpper(), search)) ||
                (user.Biography != null && EF.Functions.Like(user.Biography.ToUpper(), search)) ||
                     EF.Functions.Like(user.Language.ToUpper(), search));
        }

        return await result.CountAsync();
    }
}