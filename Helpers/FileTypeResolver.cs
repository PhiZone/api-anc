using Microsoft.AspNetCore.StaticFiles;

namespace PhiZoneApi.Helpers
{
    public static class FileTypeResolver
    {
        public static string GetMimeType(IFormFile file)
        {
            string mimeType = "application/octet-stream";
            var provider = new FileExtensionContentTypeProvider();
            if (provider.TryGetContentType(file.FileName, out var resolvedMimeType))
            {
                mimeType = resolvedMimeType;
            }

            return mimeType;
        }
        public static string GetFileExtension(string mimeType)
        {
            var provider = new FileExtensionContentTypeProvider();

            string[] extensions = provider.Mappings
                .Where(mapping => string.Equals(mapping.Value, mimeType, StringComparison.OrdinalIgnoreCase))
                .Select(mapping => mapping.Key)
                .ToArray();

            if (extensions.Length > 0)
            {
                return extensions[0];
            }

            return string.Empty;
        }
    }
}
