namespace PhiZoneApi.Interfaces;

public interface ISongService
{
    Task<(string, TimeSpan)?> UploadAsync(string fileName, IFormFile file);
    
    Task<(string, TimeSpan)?> UploadAsync(string fileName, byte[] file);

    Task PublishAsync(IFormFile file, Guid songId);
}