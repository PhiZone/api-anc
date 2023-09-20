using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class ReplyRepository : IReplyRepository
{
    private readonly ApplicationDbContext _context;

    public ReplyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Reply>> GetRepliesAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Reply, bool>>? predicate = null)
    {
        var result = _context.Replies.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Reply> GetReplyAsync(Guid id)
    {
        return (await _context.Replies.FirstOrDefaultAsync(like => like.Id == id))!;
    }

    public async Task<bool> ReplyExistsAsync(Guid id)
    {
        return await _context.Replies.AnyAsync(like => like.Id == id);
    }

    public async Task<bool> CreateReplyAsync(Reply reply)
    {
        await _context.Replies.AddAsync(reply);
        return await SaveAsync();
    }

    public async Task<bool> UpdateReplyAsync(Reply reply)
    {
        _context.Replies.Update(reply);
        return await SaveAsync();
    }

    public async Task<bool> RemoveReplyAsync(Guid id)
    {
        _context.Replies.Remove((await _context.Replies.FirstOrDefaultAsync(reply => reply.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountRepliesAsync(Expression<Func<Reply, bool>>? predicate = null)
    {
        var result = _context.Replies.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}