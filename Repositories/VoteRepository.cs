using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class VoteRepository : IVoteRepository
{
    private readonly ApplicationDbContext _context;

    public VoteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Vote>> GetVotesAsync(string order, bool desc, int position, int take,
        Expression<Func<Vote, bool>>? predicate = null)
    {
        var result = _context.Votes.OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<Vote> GetVoteAsync(Guid id)
    {
        return (await _context.Votes.FirstOrDefaultAsync(vote => vote.Id == id))!;
    }

    public async Task<Vote> GetVoteAsync(Guid chartId, int userId)
    {
        return (await _context.Votes.FirstOrDefaultAsync(
            vote => vote.Chart.Id == chartId && vote.OwnerId == userId))!;
    }

    public async Task<bool> VoteExistsAsync(Guid id)
    {
        return (await _context.Votes.AnyAsync(vote => vote.Id == id))!;
    }

    public async Task<bool> VoteExistsAsync(Guid chartId, int userId)
    {
        return (await _context.Votes.AnyAsync(
            vote => vote.Chart.Id == chartId && vote.OwnerId == userId))!;
    }

    public async Task<bool> CreateVoteAsync(Vote vote)
    {
        await _context.Votes.AddAsync(vote);
        return await SaveAsync();
    }

    public async Task<bool> RemoveVoteAsync(Guid id)
    {
        _context.Votes.Remove((await _context.Votes.FirstOrDefaultAsync(vote => vote.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountVotesAsync(Expression<Func<Vote, bool>>? predicate = null)
    {
        var result = _context.Votes.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}