namespace PhiZoneApi.Interfaces;

public interface IFileStorageService
{
    Task<string> Upload<T>(string fileName, IFormFile formFile);
    
    Task<string> UploadImage<T>(string fileName, IFormFile formFile, (int, int) aspectRatio);
    
    Task<string> UploadImage<T>(string fileName, byte[] buffer, (int, int) aspectRatio);
    
    Task<string> Upload<T>(string fileName, MemoryStream stream, string extension);
}