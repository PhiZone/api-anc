namespace PhiZoneApi.Interfaces;

public interface IFileStorageService
{
    Task<string> Upload(string fileName, IFormFile formFile);
}