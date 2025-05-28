using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class PetQuestionRepository(ApplicationDbContext context, IMeilisearchService meilisearchService)
    : IPetQuestionRepository
{
    public async Task<ICollection<PetQuestion>> GetPetQuestionsAsync(List<string>? order = null,
        List<bool>? desc = null, int? position = 0,
        int? take = -1,
        Expression<Func<PetQuestion, bool>>? predicate = null)
    {
        var result = context.PetQuestions.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<PetQuestion> GetPetQuestionAsync(Guid id)
    {
        return (await context.PetQuestions.FirstOrDefaultAsync(petQuestion => petQuestion.Id == id))!;
    }

    public async Task<PetQuestion?> GetRandomPetQuestionAsync(int position, string language)
    {
        return await context.PetQuestions
            .Where(petQuestion => petQuestion.Position == position && petQuestion.Language == language)
            .OrderBy(e => EF.Functions.Random())
            .FirstOrDefaultAsync();
    }

    public async Task<bool> PetQuestionExistsAsync(Guid id)
    {
        return await context.PetQuestions.AnyAsync(petQuestion => petQuestion.Id == id);
    }

    public async Task<bool> CreatePetQuestionAsync(PetQuestion petQuestion)
    {
        await context.PetQuestions.AddAsync(petQuestion);
        await meilisearchService.AddAsync(petQuestion);
        return await SaveAsync();
    }

    public async Task<bool> UpdatePetQuestionAsync(PetQuestion petQuestion)
    {
        context.PetQuestions.Update(petQuestion);
        await meilisearchService.UpdateAsync(petQuestion);
        return await SaveAsync();
    }

    public async Task<bool> RemovePetQuestionAsync(Guid id)
    {
        context.PetQuestions.Remove(
            (await context.PetQuestions.FirstOrDefaultAsync(petQuestion => petQuestion.Id == id))!);
        await meilisearchService.DeleteAsync<PetQuestion>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountPetQuestionsAsync(
        Expression<Func<PetQuestion, bool>>? predicate = null)
    {
        var result = context.PetQuestions.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }

    public async Task<ICollection<PetChoice>> GetQuestionChoicesAsync(Guid questionId)
    {
        return await context.PetChoices.Where(petChoice => petChoice.QuestionId == questionId)
            .OrderBy(petChoice => EF.Functions.Random())
            .ToArrayAsync();
    }
}