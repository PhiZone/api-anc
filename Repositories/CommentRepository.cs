using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly ApplicationDbContext _context;

    public CommentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Comment>> GetCommentsAsync(List<string> order, List<bool> desc, int position,
        int take,
        Expression<Func<Comment, bool>>? predicate = null)
    {
        var result = _context.Comments.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Comment> GetCommentAsync(Guid id)
    {
        return (await _context.Comments.FirstOrDefaultAsync(like => like.Id == id))!;
    }

    public async Task<ICollection<Reply>> GetCommentRepliesAsync(Guid id, List<string> order, List<bool> desc,
        int position,
        int take, Expression<Func<Reply, bool>>? predicate = null)
    {
        var comment = (await _context.Comments.FirstOrDefaultAsync(comment => comment.Id == id))!;
        var result = _context.Replies.Where(record => record.Comment.Id == comment.Id).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<bool> CommentExistsAsync(Guid id)
    {
        return await _context.Comments.AnyAsync(like => like.Id == id);
    }

    public async Task<bool> CreateCommentAsync(Comment comment)
    {
        await _context.Comments.AddAsync(comment);
        return await SaveAsync();
    }

    public async Task<bool> UpdateCommentAsync(Comment comment)
    {
        _context.Comments.Update(comment);
        return await SaveAsync();
    }

    public async Task<bool> RemoveCommentAsync(Guid id)
    {
        _context.Comments.Remove((await _context.Comments.FirstOrDefaultAsync(comment => comment.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountCommentsAsync(Expression<Func<Comment, bool>>? predicate = null)
    {
        var result = _context.Comments.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }

    public async Task<int> CountCommentRepliesAsync(Guid id, Expression<Func<Reply, bool>>? predicate = null)
    {
        var comment = (await _context.Comments.FirstOrDefaultAsync(comment => comment.Id == id))!;
        var result = _context.Replies.Where(record => record.Comment.Id == comment.Id);

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}