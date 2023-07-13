using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface ILikeService
{
    Task<bool> CreateLikeAsync(Chapter chapter, int userId);

    Task<bool> CreateLikeAsync(Song song, int userId);

    Task<bool> CreateLikeAsync(Chart chart, int userId);

    Task<bool> CreateLikeAsync(Record record, int userId);

    Task<bool> CreateLikeAsync(Comment comment, int userId);

    Task<bool> CreateLikeAsync(Reply reply, int userId);

    Task<bool> CreateLikeAsync(Application application, int userId);

    Task<bool> CreateLikeAsync(Announcement announcement, int userId);

    Task<bool> RemoveLikeAsync(Chapter chapter, int userId);

    Task<bool> RemoveLikeAsync(Song song, int userId);

    Task<bool> RemoveLikeAsync(Chart chart, int userId);

    Task<bool> RemoveLikeAsync(Record record, int userId);

    Task<bool> RemoveLikeAsync(Comment comment, int userId);

    Task<bool> RemoveLikeAsync(Reply reply, int userId);

    Task<bool> RemoveLikeAsync(Application application, int userId);

    Task<bool> RemoveLikeAsync(Announcement announcement, int userId);
}