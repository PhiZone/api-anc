namespace PhiZoneApi.Interfaces;

public interface ISongService
{
    Task<(string, string, TimeSpan)?> UploadAsync(string fileName, IFormFile file);

    Task<(string, string, TimeSpan)?> UploadAsync(string fileName, byte[] file);

    Task PublishAsync(IFormFile file, Guid songId, bool isSubmission = false);
}