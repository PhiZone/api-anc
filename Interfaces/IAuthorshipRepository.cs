using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IAuthorshipRepository
{
    Task<ICollection<Authorship>> GetResourcesAsync(int authorId, string order, bool desc, int position,
        int take, Expression<Func<Authorship, bool>>? predicate = null);

    Task<ICollection<Authorship>> GetAuthorsAsync(Guid resourceId, string order, bool desc, int position,
        int take, Expression<Func<Authorship, bool>>? predicate = null);

    Task<ICollection<Authorship>> GetAuthorshipsAsync(string order, bool desc, int position, int take,
        Expression<Func<Authorship, bool>>? predicate = null);

    Task<Authorship> GetAuthorshipAsync(Guid resourceId, int authorId);

    Task<Authorship> GetAuthorshipAsync(Guid id);

    Task<bool> CreateAuthorshipAsync(Authorship authorship);

    Task<bool> UpdateAuthorshipAsync(Authorship authorship);

    Task<bool> RemoveAuthorshipAsync(Guid resourceId, int authorId);

    Task<bool> RemoveAuthorshipAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountAuthorshipsAsync(Expression<Func<Authorship, bool>>? predicate = null);

    Task<bool> AuthorshipExistsAsync(Guid resourceId, int authorId);

    Task<bool> AuthorshipExistsAsync(Guid id);

    Task<int> CountResourcesAsync(int authorId, Expression<Func<Authorship, bool>>? predicate = null);

    Task<int> CountAuthorsAsync(Guid resourceId, Expression<Func<Authorship, bool>>? predicate = null);
}