using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class RegionRepository : IRegionRepository
{
    private readonly ApplicationDbContext _context;

    public RegionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Region>> GetRegionsAsync(string order, bool desc, int position, int take)
    {
        return await _context.Regions.OrderBy(order, desc).Skip(position).Take(take).ToListAsync();
    }

    public async Task<Region> GetRegionByIdAsync(int id)
    {
        return (await _context.Regions.FirstOrDefaultAsync(region => region.Id == id))!;
    }

    public async Task<Region> GetRegionAsync(string code)
    {
        return (await _context.Regions.FirstOrDefaultAsync(region => string.Equals(region.Code, code.ToUpper())))!;
    }

    public async Task<ICollection<User>> GetRegionUsersByIdAsync(int id, string order, bool desc, int position,
        int take)
    {
        var region = (await _context.Regions.FirstOrDefaultAsync(region => region.Id == id))!;
        return await _context.Users.Where(user => user.Region == region)
            .OrderBy(order, desc)
            .Skip(position)
            .Take(take)
            .ToListAsync();
    }

    public async Task<ICollection<User>> GetRegionUsersAsync(string code, string order, bool desc, int position,
        int take)
    {
        var region = (await _context.Regions.FirstOrDefaultAsync(region =>
            string.Equals(region.Code, code.ToUpper())))!;
        return await _context.Users.Where(user => user.Region == region)
            .OrderBy(order, desc)
            .Skip(position)
            .Take(take)
            .ToListAsync();
    }

    public async Task<bool> RegionExistsByIdAsync(int id)
    {
        return await _context.Regions.AnyAsync(region => region.Id == id);
    }

    public async Task<bool> RegionExistsAsync(string code)
    {
        return await _context.Regions.AnyAsync(region => string.Equals(region.Code, code.ToUpper()));
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

    public async Task<bool> DeleteRegionAsync(Region region)
    {
        _context.Remove(region);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountAsync()
    {
        return await _context.Regions.CountAsync();
    }

    public async Task<int> CountUsersAsync(string code)
    {
        var region = (await _context.Regions.FirstOrDefaultAsync(region =>
            string.Equals(region.Code, code.ToUpper())))!;
        return await _context.Users.Where(user => user.Region == region).CountAsync();
    }

    public async Task<int> CountUsersByIdAsync(int id)
    {
        var region = (await _context.Regions.FirstOrDefaultAsync(region => region.Id == id))!;
        return await _context.Users.Where(user => user.Region == region).CountAsync();
    }
}