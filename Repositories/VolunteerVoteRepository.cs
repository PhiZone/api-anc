using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class VolunteerVoteRepository(ApplicationDbContext context) : IVolunteerVoteRepository
{
    public async Task<ICollection<VolunteerVote>> GetVolunteerVotesAsync(List<string> order, List<bool> desc,
        int position,
        int take,
        Expression<Func<VolunteerVote, bool>>? predicate = null)
    {
        var result = context.VolunteerVotes.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<VolunteerVote> GetVolunteerVoteAsync(Guid id)
    {
        return (await context.VolunteerVotes.FirstOrDefaultAsync(vote => vote.Id == id))!;
    }

    public async Task<VolunteerVote> GetVolunteerVoteAsync(Guid chartId, int userId)
    {
        return (await context.VolunteerVotes.FirstOrDefaultAsync(
            vote => vote.Chart.Id == chartId && vote.OwnerId == userId))!;
    }

    public async Task<bool> VolunteerVoteExistsAsync(Guid id)
    {
        return (await context.VolunteerVotes.AnyAsync(vote => vote.Id == id))!;
    }

    public async Task<bool> VolunteerVoteExistsAsync(Guid chartId, int userId)
    {
        return (await context.VolunteerVotes.AnyAsync(
            vote => vote.Chart.Id == chartId && vote.OwnerId == userId))!;
    }

    public async Task<bool> CreateVolunteerVoteAsync(VolunteerVote vote)
    {
        await context.VolunteerVotes.AddAsync(vote);
        return await SaveAsync();
    }

    public async Task<bool> UpdateVolunteerVoteAsync(VolunteerVote vote)
    {
        context.VolunteerVotes.Update(vote);
        return await SaveAsync();
    }

    public async Task<bool> RemoveVolunteerVoteAsync(Guid id)
    {
        context.VolunteerVotes.Remove((await context.VolunteerVotes.FirstOrDefaultAsync(vote => vote.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> RemoveVolunteerVotesAsync(IEnumerable<VolunteerVote> votes)
    {
        context.VolunteerVotes.RemoveRange(votes);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountVolunteerVotesAsync(Expression<Func<VolunteerVote, bool>>? predicate = null)
    {
        var result = context.VolunteerVotes.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}