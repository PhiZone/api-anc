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
        var extension = FileTypeResolver.GetFileExtension(FileTypeResolver.GetMimeType(formFile));
        var file = new LCFile($"{fileName}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension}",
            memoryStream.ToArray());
        await file.Save();
        return file.Url;
    }

    public async Task<string> Upload(string fileName, MemoryStream stream, string extension)
    {
        var file = new LCFile($"{fileName}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.{extension}",
            stream.ToArray());
        await file.Save();
        return file.Url;
    }
}