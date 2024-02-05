using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class RegionRepository(ApplicationDbContext context, IMeilisearchService meilisearchService) : IRegionRepository
{
    public async Task<ICollection<Region>> GetRegionsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Region, bool>>? predicate = null)
    {
        var result = context.Regions.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
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
        int take, Expression<Func<User, bool>>? predicate = null, int? currentUserId = null)
    {
        var region = (await context.Regions.FirstOrDefaultAsync(region => region.Id == id))!;
        var result = context.Users.Where(user => user.Region == region).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.FollowerRelations.Where(relation =>
                    relation.FollowerId == currentUserId && relation.Type != UserRelationType.Blacklisted)
                .Take(1));
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<User>> GetRegionUsersAsync(string code, List<string> order, List<bool> desc,
        int position,
        int take, Expression<Func<User, bool>>? predicate = null, int? currentUserId = null)
    {
        var region = (await context.Regions.FirstOrDefaultAsync(region =>
            string.Equals(region.Code, code.ToUpper())))!;
        var result = context.Users.Where(user => user.Region == region).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.FollowerRelations.Where(relation =>
                    relation.FollowerId == currentUserId && relation.Type != UserRelationType.Blacklisted)
                .Take(1));
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
        await meilisearchService.AddAsync(region);
        return await SaveAsync();
    }

    public async Task<bool> UpdateRegionAsync(Region region)
    {
        context.Regions.Update(region);
        await meilisearchService.UpdateAsync(region);
        return await SaveAsync();
    }

    public async Task<bool> RemoveRegionAsync(string code)
    {
        var region = (await context.Regions.FirstOrDefaultAsync(region => region.Code == code.ToUpper()))!;
        await meilisearchService.DeleteAsync<Region>(region.Id);
        context.Regions.Remove(region);
        return await SaveAsync();
    }

    public async Task<bool> RemoveRegionAsync(int id)
    {
        context.Regions.Remove((await context.Regions.FirstOrDefaultAsync(region => region.Id == id))!);
        await meilisearchService.DeleteAsync<Region>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountRegionsAsync(Expression<Func<Region, bool>>? predicate = null)
    {
        var result = context.Regions.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }

    public async Task<int> CountRegionUsersAsync(string code,
        Expression<Func<User, bool>>? predicate = null)
    {
        var region = (await context.Regions.FirstOrDefaultAsync(region =>
            string.Equals(region.Code, code.ToUpper())))!;
        var result = context.Users.Where(user => user.Region == region);
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }

    public async Task<int> CountRegionUsersAsync(int id,
        Expression<Func<User, bool>>? predicate = null)
    {
        var region = (await context.Regions.FirstOrDefaultAsync(region => region.Id == id))!;
        var result = context.Users.Where(user => user.Region == region);
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}