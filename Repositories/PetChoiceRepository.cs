using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class PetChoiceRepository
    (ApplicationDbContext context, IMeilisearchService meilisearchService) : IPetChoiceRepository
{
    public async Task<ICollection<PetChoice>> GetPetChoicesAsync(List<string> order, List<bool> desc, int position,
        int take,
        Expression<Func<PetChoice, bool>>? predicate = null)
    {
        var result = context.PetChoices.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
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
        await meilisearchService.AddAsync(petChoice);
        return await SaveAsync();
    }

    public async Task<bool> UpdatePetChoiceAsync(PetChoice petChoice)
    {
        context.PetChoices.Update(petChoice);
        await meilisearchService.UpdateAsync(petChoice);
        return await SaveAsync();
    }

    public async Task<bool> RemovePetChoiceAsync(Guid id)
    {
        context.PetChoices.Remove(
            (await context.PetChoices.FirstOrDefaultAsync(petChoice => petChoice.Id == id))!);
        await meilisearchService.DeleteAsync<PetChoice>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountPetChoicesAsync(
        Expression<Func<PetChoice, bool>>? predicate = null)
    {
        var result = context.PetChoices.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}