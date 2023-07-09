using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface ILikeService
{
    Task CreateLikeAsync(Chapter chapter, int userId);
    
    Task CreateLikeAsync(Song song, int userId);
    
    Task CreateLikeAsync(Chart chart, int userId);
    
    Task CreateLikeAsync(Record record, int userId);
    
    Task CreateLikeAsync(Comment comment, int userId);
    
    Task CreateLikeAsync(Reply reply, int userId);
}