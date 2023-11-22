using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class ReplyRepository(ApplicationDbContext context) : IReplyRepository
{
    public async Task<ICollection<Reply>> GetRepliesAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Reply, bool>>? predicate = null)
    {
        var result = context.Replies.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Reply> GetReplyAsync(Guid id)
    {
        return (await context.Replies.FirstOrDefaultAsync(like => like.Id == id))!;
    }

    public async Task<bool> ReplyExistsAsync(Guid id)
    {
        return await context.Replies.AnyAsync(like => like.Id == id);
    }

    public async Task<bool> CreateReplyAsync(Reply reply)
    {
        await context.Replies.AddAsync(reply);
        return await SaveAsync();
    }

    public async Task<bool> UpdateReplyAsync(Reply reply)
    {
        context.Replies.Update(reply);
        return await SaveAsync();
    }

    public async Task<bool> RemoveReplyAsync(Guid id)
    {
        context.Replies.Remove((await context.Replies.FirstOrDefaultAsync(reply => reply.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountRepliesAsync(Expression<Func<Reply, bool>>? predicate = null)
    {
        var result = context.Replies.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}