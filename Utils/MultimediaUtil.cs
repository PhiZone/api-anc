using System.Diagnostics;

namespace PhiZoneApi.Utils;

public static class MultimediaUtil
{
    public static MemoryStream CropImage(IFormFile file, (int, int) aspectRatio)
    {
        return CropImage(Image.Load(file.OpenReadStream()), aspectRatio);
    }

    public static MemoryStream CropImage(byte[] buffer, (int, int) aspectRatio)
    {
        return CropImage(Image.Load(buffer), aspectRatio);
    }

    private static MemoryStream CropImage(Image image, (int, int) aspectRatio)
    {
        var originalWidth = image.Width;
        var originalHeight = image.Height;
        var targetWidth = originalWidth;
        var targetHeight = targetWidth * aspectRatio.Item2 / aspectRatio.Item1;
        if (targetHeight > originalHeight)
        {
            targetHeight = originalHeight;
            targetWidth = targetHeight * aspectRatio.Item1 / aspectRatio.Item2;
        }

        image.Mutate(operation =>
        {
            operation.Resize(new ResizeOptions
            {
                Size = new Size(targetWidth, targetHeight), Mode = ResizeMode.Crop
            });
        });

        var stream = new MemoryStream();
        image.SaveAsWebpAsync(stream);
        return stream;
    }

    public static async Task<MemoryStream?> ConvertAudio(IFormFile file)
    {
        var tempInputPath = Path.GetTempFileName();
        try
        {
            await using (var stream = new FileStream(tempInputPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var outputStream = ConvertToStream(tempInputPath);
            File.Delete(tempInputPath);
            return outputStream;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }

    public static async Task<MemoryStream?> ConvertAudio(byte[] buffer)
    {
        var tempInputPath = Path.GetTempFileName();
        try
        {
            await using (var stream = new FileStream(tempInputPath, FileMode.Create))
            {
                await stream.WriteAsync(buffer);
            }

            var outputStream = ConvertToStream(tempInputPath);
            File.Delete(tempInputPath);
            return outputStream;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }

    private static MemoryStream ConvertToStream(string inputFilePath)
    {
        var outputStream = new MemoryStream();

        const string ffmpegPath = "ffmpeg";
        const string outputOptions = "-vn -c:a libvorbis -q:a 0 -f ogg -map_metadata 0 pipe:1";
        var arguments = $"-i \"{inputFilePath}\" {outputOptions}";

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = new Process())
        {
            process.StartInfo = startInfo;
            process.Start();

            using (var output = process.StandardOutput.BaseStream)
            {
                var buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = output.Read(buffer, 0, buffer.Length)) > 0)
                    outputStream.Write(buffer, 0, bytesRead);
            }

            process.WaitForExit();
        }

        outputStream.Position = 0;

        return outputStream;
    }
}