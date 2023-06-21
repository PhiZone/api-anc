using LeanCloud;
using LeanCloud.Storage;
using Microsoft.Extensions.Options;
using PhiZoneApi.Configurations;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Services;

public class FileStorageService : IFileStorageService
{
    public FileStorageService(IOptions<FileStorageSettings> options)
    {
        LCApplication.Initialize(options.Value.ClientId, options.Value.ClientToken, options.Value.ServerUrl);
    }

    public async Task<string> Upload(string fileName, IFormFile formFile)
    {
        using var memoryStream = new MemoryStream();
        await formFile.CopyToAsync(memoryStream);
        var file = new LCFile(
            string.Join("_", fileName, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) +
            FileTypeResolver.GetFileExtension(FileTypeResolver.GetMimeType(formFile)),
            memoryStream.ToArray()
        );
        await file.Save();
        return file.Url;
    }
}