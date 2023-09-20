using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class CollaborationRepository : ICollaborationRepository
{
    private readonly ApplicationDbContext _context;

    public CollaborationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Collaboration>> GetCollaborationsAsync(List<string> order, List<bool> desc,
        int position,
        int take, Expression<Func<Collaboration, bool>>? predicate = null)
    {
        var result = _context.Collaborations.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Collaboration> GetCollaborationAsync(Guid id)
    {
        return (await _context.Collaborations.FirstOrDefaultAsync(collaboration => collaboration.Id == id))!;
    }

    public async Task<Collaboration> GetCollaborationAsync(Guid submissionId, int inviteeId)
    {
        return (await _context.Collaborations.FirstOrDefaultAsync(collaboration =>
            collaboration.SubmissionId == submissionId && collaboration.InviteeId == inviteeId))!;
    }

    public async Task<bool> CreateCollaborationAsync(Collaboration collaboration)
    {
        await _context.Collaborations.AddAsync(collaboration);
        return await SaveAsync();
    }

    public async Task<bool> UpdateCollaborationAsync(Collaboration collaboration)
    {
        _context.Collaborations.Update(collaboration);
        return await SaveAsync();
    }

    public async Task<bool> RemoveCollaborationAsync(Guid id)
    {
        _context.Collaborations.Remove(
            (await _context.Collaborations.FirstOrDefaultAsync(collaboration => collaboration.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountCollaborationsAsync(Expression<Func<Collaboration, bool>>? predicate = null)
    {
        if (predicate != null) return await _context.Collaborations.Where(predicate).CountAsync();
        return await _context.Collaborations.CountAsync();
    }

    public async Task<bool> CollaborationExistsAsync(Guid id)
    {
        return await _context.Collaborations.AnyAsync(collaboration => collaboration.Id == id);
    }

    public async Task<bool> CollaborationExistsAsync(Guid submissionId, int inviteeId)
    {
        return await _context.Collaborations.AnyAsync(collaboration =>
            collaboration.SubmissionId == submissionId && collaboration.InviteeId == inviteeId);
    }
}