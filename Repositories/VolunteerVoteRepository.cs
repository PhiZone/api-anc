using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class VolunteerVoteRepository : IVolunteerVoteRepository
{
    private readonly ApplicationDbContext _context;

    public VolunteerVoteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<VolunteerVote>> GetVolunteerVotesAsync(string order, bool desc, int position,
        int take,
        Expression<Func<VolunteerVote, bool>>? predicate = null)
    {
        var result = _context.VolunteerVotes.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<VolunteerVote> GetVolunteerVoteAsync(Guid id)
    {
        return (await _context.VolunteerVotes.FirstOrDefaultAsync(vote => vote.Id == id))!;
    }

    public async Task<VolunteerVote> GetVolunteerVoteAsync(Guid chartId, int userId)
    {
        return (await _context.VolunteerVotes.FirstOrDefaultAsync(
            vote => vote.Chart.Id == chartId && vote.OwnerId == userId))!;
    }

    public async Task<bool> VolunteerVoteExistsAsync(Guid id)
    {
        return (await _context.VolunteerVotes.AnyAsync(vote => vote.Id == id))!;
    }

    public async Task<bool> VolunteerVoteExistsAsync(Guid chartId, int userId)
    {
        return (await _context.VolunteerVotes.AnyAsync(
            vote => vote.Chart.Id == chartId && vote.OwnerId == userId))!;
    }

    public async Task<bool> CreateVolunteerVoteAsync(VolunteerVote vote)
    {
        await _context.VolunteerVotes.AddAsync(vote);
        return await SaveAsync();
    }

    public async Task<bool> UpdateVolunteerVoteAsync(VolunteerVote vote)
    {
        _context.VolunteerVotes.Update(vote);
        return await SaveAsync();
    }

    public async Task<bool> RemoveVolunteerVoteAsync(Guid id)
    {
        _context.VolunteerVotes.Remove((await _context.VolunteerVotes.FirstOrDefaultAsync(vote => vote.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> RemoveVolunteerVotesAsync(IEnumerable<VolunteerVote> votes)
    {
        _context.VolunteerVotes.RemoveRange(votes);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountVolunteerVotesAsync(Expression<Func<VolunteerVote, bool>>? predicate = null)
    {
        var result = _context.VolunteerVotes.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}