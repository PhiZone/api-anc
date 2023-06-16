using LeanCloud.Storage;

namespace PhiZoneApi.Helpers
{
    public static class FileUploader
    {

        public async static Task<string> Upload(string fileName, string extension, byte[] bytes)
        {
            var file = new LCFile(
                string.Join("_", fileName, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) + extension,
                bytes
            );
            await file.Save();
            return file.Url;
        }

        public async static Task<string> Upload(string fileName, IFormFile formFile)
        {
            using var memoryStream = new MemoryStream();
            await formFile.CopyToAsync(memoryStream);
            return await Upload(
                fileName,
                FileTypeResolver.GetFileExtension(FileTypeResolver.GetMimeType(formFile)),
                memoryStream.ToArray()
            );
        }

    }
}
