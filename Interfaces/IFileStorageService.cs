namespace PhiZoneApi.Interfaces;

public interface IFileStorageService
{
    Task<string> Upload(string fileName, IFormFile formFile);
    Task<string> Upload(string fileName, MemoryStream stream, string extension);
}