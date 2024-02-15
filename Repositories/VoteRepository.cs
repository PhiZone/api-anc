using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class VoteRepository(ApplicationDbContext context) : IVoteRepository
{
    public async Task<ICollection<Vote>> GetVotesAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Vote, bool>>? predicate = null)
    {
        var result = context.Votes.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Vote> GetVoteAsync(Guid id)
    {
        return (await context.Votes.FirstOrDefaultAsync(vote => vote.Id == id))!;
    }

    public async Task<Vote> GetVoteAsync(Guid chartId, int userId)
    {
        return (await context.Votes.FirstOrDefaultAsync(
            vote => vote.Chart.Id == chartId && vote.OwnerId == userId))!;
    }

    public async Task<bool> VoteExistsAsync(Guid id)
    {
        return (await context.Votes.AnyAsync(vote => vote.Id == id))!;
    }

    public async Task<bool> VoteExistsAsync(Guid chartId, int userId)
    {
        return (await context.Votes.AnyAsync(
            vote => vote.Chart.Id == chartId && vote.OwnerId == userId))!;
    }

    public async Task<bool> CreateVoteAsync(Vote vote)
    {
        await context.Votes.AddAsync(vote);
        return await SaveAsync();
    }

    public async Task<bool> UpdateVoteAsync(Vote vote)
    {
        context.Votes.Update(vote);
        return await SaveAsync();
    }

    public async Task<bool> RemoveVoteAsync(Guid id)
    {
        context.Votes.Remove((await context.Votes.FirstOrDefaultAsync(vote => vote.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountVotesAsync(Expression<Func<Vote, bool>>? predicate = null)
    {
        var result = context.Votes.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}