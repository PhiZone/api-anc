using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class PetQuestionRepository : IPetQuestionRepository
{
    private readonly ApplicationDbContext _context;

    public PetQuestionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<PetQuestion>> GetPetQuestionsAsync(string order, bool desc, int position, int take,
        string? search = null, Expression<Func<PetQuestion, bool>>? predicate = null)
    {
        var result = _context.PetQuestions.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(petQuestion => EF.Functions.Like(petQuestion.Content.ToUpper(), search));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<PetQuestion> GetPetQuestionAsync(Guid id)
    {
        return (await _context.PetQuestions.FirstOrDefaultAsync(petQuestion => petQuestion.Id == id))!;
    }

    public async Task<PetQuestion?> GetRandomPetQuestionAsync(int position, string language)
    {
        return await _context.PetQuestions
            .Where(petQuestion => petQuestion.Position == position && petQuestion.Language == language)
            .OrderBy(e => EF.Functions.Random())
            .FirstOrDefaultAsync();
    }

    public async Task<bool> PetQuestionExistsAsync(Guid id)
    {
        return await _context.PetQuestions.AnyAsync(petQuestion => petQuestion.Id == id);
    }

    public async Task<bool> CreatePetQuestionAsync(PetQuestion petQuestion)
    {
        await _context.PetQuestions.AddAsync(petQuestion);
        return await SaveAsync();
    }

    public async Task<bool> UpdatePetQuestionAsync(PetQuestion petQuestion)
    {
        _context.PetQuestions.Update(petQuestion);
        return await SaveAsync();
    }

    public async Task<bool> RemovePetQuestionAsync(Guid id)
    {
        _context.PetQuestions.Remove(
            (await _context.PetQuestions.FirstOrDefaultAsync(petQuestion => petQuestion.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountPetQuestionsAsync(string? search = null,
        Expression<Func<PetQuestion, bool>>? predicate = null)
    {
        var result = _context.PetQuestions.AsQueryable();

        if (predicate != null) result = result.Where(predicate);
        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(petQuestion => EF.Functions.Like(petQuestion.Content.ToUpper(), search));
        }

        return await result.CountAsync();
    }

    public async Task<ICollection<PetChoice>> GetQuestionChoicesAsync(Guid questionId)
    {
        return await _context.PetChoices.Where(petChoice => petChoice.QuestionId == questionId)
            .OrderBy(petChoice => EF.Functions.Random())
            .ToArrayAsync();
    }
}