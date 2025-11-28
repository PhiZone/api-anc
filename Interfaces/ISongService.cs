namespace PhiZoneApi.Interfaces;

public interface ISongService
{
    Task<(string, string, TimeSpan)?> UploadAsync(string fileName, IFormFile file);

    Task<(string, string, TimeSpan)?> UploadAsync(string fileName, byte[] buffer);

    Task<(string, string, TimeSpan)?> UploadAsync(string fileName, string filePath);

    Task PublishAsync(IFormFile file, Guid songId, bool isSubmission = false, bool burn = true, string? filePath = null);
}