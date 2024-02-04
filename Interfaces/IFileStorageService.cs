using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IFileStorageService
{
    Task<(string, string)> Upload<T>(string fileName, IFormFile formFile);

    Task<(string, string)> UploadImage<T>(string fileName, IFormFile formFile, (int, int) aspectRatio);

    Task<(string, string)> UploadImage<T>(string fileName, byte[] buffer, (int, int) aspectRatio);

    Task<(string, string)> Upload<T>(string fileName, MemoryStream stream, string extension);

    Task SendUserInput(string url, string memberName, HttpRequest request, User user);
}