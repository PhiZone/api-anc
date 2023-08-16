using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class PetChoiceRepository : IPetChoiceRepository
{
    private readonly ApplicationDbContext _context;

    public PetChoiceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<PetChoice>> GetPetChoicesAsync(string order, bool desc, int position, int take,
        string? search = null, Expression<Func<PetChoice, bool>>? predicate = null)
    {
        var result = _context.PetChoices.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(petChoice => petChoice.Content.ToUpper().Like(search));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<PetChoice> GetPetChoiceAsync(Guid id)
    {
        return (await _context.PetChoices.FirstOrDefaultAsync(petChoice => petChoice.Id == id))!;
    }
    
    public async Task<bool> PetChoiceExistsAsync(Guid id)
    {
        return await _context.PetChoices.AnyAsync(petChoice => petChoice.Id == id);
    }

    public async Task<bool> CreatePetChoiceAsync(PetChoice petChoice)
    {
        await _context.PetChoices.AddAsync(petChoice);
        return await SaveAsync();
    }

    public async Task<bool> UpdatePetChoiceAsync(PetChoice petChoice)
    {
        _context.PetChoices.Update(petChoice);
        return await SaveAsync();
    }

    public async Task<bool> RemovePetChoiceAsync(Guid id)
    {
        _context.PetChoices.Remove(
            (await _context.PetChoices.FirstOrDefaultAsync(petChoice => petChoice.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountPetChoicesAsync(string? search = null,
        Expression<Func<PetChoice, bool>>? predicate = null)
    {
        var result = _context.PetChoices.AsQueryable();

        if (predicate != null) result = result.Where(predicate);
        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(petChoice => petChoice.Content.ToUpper().Like(search));
        }

        return await result.CountAsync();
    }
}