using Microsoft.AspNetCore.StaticFiles;

namespace PhiZoneApi.Utils;

public static class FileTypeResolver
{
    public static string GetMimeType(IFormFile file)
    {
        var mimeType = "application/octet-stream";
        var provider = new FileExtensionContentTypeProvider();
        if (provider.TryGetContentType(file.FileName, out var resolvedMimeType)) mimeType = resolvedMimeType;

        return mimeType;
    }

    public static string GetFileExtension(string mimeType)
    {
        var provider = new FileExtensionContentTypeProvider();

        var extensions = provider.Mappings
            .Where(mapping => string.Equals(mapping.Value, mimeType, StringComparison.OrdinalIgnoreCase))
            .Select(mapping => mapping.Key)
            .ToArray();

        return extensions.Length > 0 ? extensions[0] : string.Empty;
    }
}