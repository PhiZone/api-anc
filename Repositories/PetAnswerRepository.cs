using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class PetAnswerRepository : IPetAnswerRepository
{
    private readonly ApplicationDbContext _context;

    public PetAnswerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<PetAnswer>> GetPetAnswersAsync(string order, bool desc, int position, int take,
        string? search = null, Expression<Func<PetAnswer, bool>>? predicate = null)
    {
        var result = _context.PetAnswers.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(petAnswer =>
                EF.Functions.Like(petAnswer.Answer1.ToUpper(), search) || EF.Functions.Like(petAnswer.Answer2.ToUpper(), search) ||
            EF.Functions.Like(petAnswer.Answer3.ToUpper(), search));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<PetAnswer> GetPetAnswerAsync(Guid id)
    {
        return (await _context.PetAnswers.FirstOrDefaultAsync(petAnswer => petAnswer.Id == id))!;
    }

    public async Task<bool> PetAnswerExistsAsync(Guid id)
    {
        return await _context.PetAnswers.AnyAsync(petAnswer => petAnswer.Id == id);
    }

    public async Task<bool> CreatePetAnswerAsync(PetAnswer petAnswer)
    {
        await _context.PetAnswers.AddAsync(petAnswer);
        return await SaveAsync();
    }

    public async Task<bool> UpdatePetAnswerAsync(PetAnswer petAnswer)
    {
        _context.PetAnswers.Update(petAnswer);
        return await SaveAsync();
    }

    public async Task<bool> RemovePetAnswerAsync(Guid id)
    {
        _context.PetAnswers.Remove((await _context.PetAnswers.FirstOrDefaultAsync(petAnswer => petAnswer.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountPetAnswersAsync(string? search = null,
        Expression<Func<PetAnswer, bool>>? predicate = null)
    {
        var result = _context.PetAnswers.AsQueryable();

        if (predicate != null) result = result.Where(predicate);
        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(petAnswer =>
                EF.Functions.Like(petAnswer.Answer1.ToUpper(), search) || EF.Functions.Like(petAnswer.Answer2.ToUpper(), search) ||
            EF.Functions.Like(petAnswer.Answer3.ToUpper(), search));
        }

        return await result.CountAsync();
    }
}