using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class PetChoiceRepository(ApplicationDbContext context) : IPetChoiceRepository
{
    public async Task<ICollection<PetChoice>> GetPetChoicesAsync(List<string> order, List<bool> desc, int position,
        int take,
        string? search = null, Expression<Func<PetChoice, bool>>? predicate = null)
    {
        var result = context.PetChoices.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(petChoice => EF.Functions.Like(petChoice.Content.ToUpper(), search));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<PetChoice> GetPetChoiceAsync(Guid id)
    {
        return (await context.PetChoices.FirstOrDefaultAsync(petChoice => petChoice.Id == id))!;
    }

    public async Task<bool> PetChoiceExistsAsync(Guid id)
    {
        return await context.PetChoices.AnyAsync(petChoice => petChoice.Id == id);
    }

    public async Task<bool> CreatePetChoiceAsync(PetChoice petChoice)
    {
        await context.PetChoices.AddAsync(petChoice);
        return await SaveAsync();
    }

    public async Task<bool> UpdatePetChoiceAsync(PetChoice petChoice)
    {
        context.PetChoices.Update(petChoice);
        return await SaveAsync();
    }

    public async Task<bool> RemovePetChoiceAsync(Guid id)
    {
        context.PetChoices.Remove(
            (await context.PetChoices.FirstOrDefaultAsync(petChoice => petChoice.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountPetChoicesAsync(string? search = null,
        Expression<Func<PetChoice, bool>>? predicate = null)
    {
        var result = context.PetChoices.AsQueryable();

        if (predicate != null) result = result.Where(predicate);
        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(petChoice => EF.Functions.Like(petChoice.Content.ToUpper(), search));
        }

        return await result.CountAsync();
    }
}