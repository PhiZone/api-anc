using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class PetAnswerRepository(ApplicationDbContext context) : IPetAnswerRepository
{
    public async Task<ICollection<PetAnswer>> GetPetAnswersAsync(List<string> order, List<bool> desc, int position,
        int take,
        string? search = null, Expression<Func<PetAnswer, bool>>? predicate = null)
    {
        var result = context.PetAnswers.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(petAnswer =>
                EF.Functions.Like(petAnswer.Answer1.ToUpper(), search) ||
                EF.Functions.Like(petAnswer.Answer2.ToUpper(), search) ||
                EF.Functions.Like(petAnswer.Answer3.ToUpper(), search));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
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
        return await SaveAsync();
    }

    public async Task<bool> UpdatePetAnswerAsync(PetAnswer petAnswer)
    {
        context.PetAnswers.Update(petAnswer);
        return await SaveAsync();
    }

    public async Task<bool> RemovePetAnswerAsync(Guid id)
    {
        context.PetAnswers.Remove((await context.PetAnswers.FirstOrDefaultAsync(petAnswer => petAnswer.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountPetAnswersAsync(string? search = null,
        Expression<Func<PetAnswer, bool>>? predicate = null)
    {
        var result = context.PetAnswers.AsQueryable();

        if (predicate != null) result = result.Where(predicate);
        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(petAnswer =>
                EF.Functions.Like(petAnswer.Answer1.ToUpper(), search) ||
                EF.Functions.Like(petAnswer.Answer2.ToUpper(), search) ||
                EF.Functions.Like(petAnswer.Answer3.ToUpper(), search));
        }

        return await result.CountAsync();
    }
}