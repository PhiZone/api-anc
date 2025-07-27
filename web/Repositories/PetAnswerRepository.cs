using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class PetAnswerRepository(ApplicationDbContext context, IMeilisearchService meilisearchService)
    : IPetAnswerRepository
{
    public async Task<ICollection<PetAnswer>> GetPetAnswersAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0,
        int? take = -1,
        Expression<Func<PetAnswer, bool>>? predicate = null)
    {
        var result = context.PetAnswers.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<PetAnswer> GetPetAnswerAsync(Guid id)
    {
        return (await context.PetAnswers.FirstOrDefaultAsync(petAnswer => petAnswer.Id == id))!;
    }

    public async Task<bool> PetAnswerExistsAsync(Guid id)
    {
        return await context.PetAnswers.AnyAsync(petAnswer => petAnswer.Id == id);
    }

    public async Task<bool> CreatePetAnswerAsync(PetAnswer petAnswer)
    {
        await context.PetAnswers.AddAsync(petAnswer);
        await meilisearchService.AddAsync(petAnswer);
        return await SaveAsync();
    }

    public async Task<bool> UpdatePetAnswerAsync(PetAnswer petAnswer)
    {
        context.PetAnswers.Update(petAnswer);
        await meilisearchService.UpdateAsync(petAnswer);
        return await SaveAsync();
    }

    public async Task<bool> UpdatePetAnswersAsync(IEnumerable<PetAnswer> petAnswers)
    {
        var list = petAnswers.ToList();
        context.PetAnswers.UpdateRange(list);
        await meilisearchService.UpdateBatchAsync(list);
        return await SaveAsync();
    }

    public async Task<bool> RemovePetAnswerAsync(Guid id)
    {
        context.PetAnswers.Remove((await context.PetAnswers.FirstOrDefaultAsync(petAnswer => petAnswer.Id == id))!);
        await meilisearchService.DeleteAsync<PetAnswer>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountPetAnswersAsync(
        Expression<Func<PetAnswer, bool>>? predicate = null)
    {
        var result = context.PetAnswers.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}