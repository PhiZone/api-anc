using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class RegionRepository(ApplicationDbContext context) : IRegionRepository
{
    public async Task<ICollection<Region>> GetRegionsAsync(List<string> order, List<bool> desc, int position, int take,
        string? search = null, Expression<Func<Region, bool>>? predicate = null)
    {
        var result = context.Regions.OrderBy(order, desc);

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
        return (await context.Regions.FirstOrDefaultAsync(region => region.Id == id))!;
    }

    public async Task<Region> GetRegionAsync(string code)
    {
        return (await context.Regions.FirstOrDefaultAsync(region => string.Equals(region.Code, code.ToUpper())))!;
    }

    public async Task<ICollection<User>> GetRegionUsersAsync(int id, List<string> order, List<bool> desc, int position,
        int take, string? search = null, Expression<Func<User, bool>>? predicate = null)
    {
        var region = (await context.Regions.FirstOrDefaultAsync(region => region.Id == id))!;
        var result = context.Users.Where(user => user.Region == region).OrderBy(order, desc);

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

    public async Task<ICollection<User>> GetRegionUsersAsync(string code, List<string> order, List<bool> desc,
        int position,
        int take, string? search = null, Expression<Func<User, bool>>? predicate = null)
    {
        var region = (await context.Regions.FirstOrDefaultAsync(region =>
            string.Equals(region.Code, code.ToUpper())))!;
        var result = context.Users.Where(user => user.Region == region).OrderBy(order, desc);

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
        return await context.Regions.AnyAsync(region => region.Id == id);
    }

    public async Task<bool> RegionExistsAsync(string code)
    {
        return await context.Regions.AnyAsync(region => string.Equals(region.Code, code.ToUpper()));
    }

    public bool RegionExists(string code)
    {
        return context.Regions.Any(region => string.Equals(region.Code, code.ToUpper()));
    }

    public async Task<bool> CreateRegionAsync(Region region)
    {
        await context.Regions.AddAsync(region);
        return await SaveAsync();
    }

    public async Task<bool> UpdateRegionAsync(Region region)
    {
        context.Regions.Update(region);
        return await SaveAsync();
    }

    public async Task<bool> RemoveRegionAsync(string code)
    {
        context.Regions.Remove((await context.Regions.FirstOrDefaultAsync(region => region.Code == code.ToUpper()))!);
        return await SaveAsync();
    }

    public async Task<bool> RemoveRegionAsync(int id)
    {
        context.Regions.Remove((await context.Regions.FirstOrDefaultAsync(region => region.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountRegionsAsync(string? search = null, Expression<Func<Region, bool>>? predicate = null)
    {
        var result = context.Regions.AsQueryable();

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
        var region = (await context.Regions.FirstOrDefaultAsync(region =>
            string.Equals(region.Code, code.ToUpper())))!;
        var result = context.Users.Where(user => user.Region == region);

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
        var region = (await context.Regions.FirstOrDefaultAsync(region => region.Id == id))!;
        var result = context.Users.Where(user => user.Region == region);

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