namespace PhiZoneApi.Utils;

public class ImageUtil
{
    public static MemoryStream CropImage(IFormFile file, (int, int) aspectRatio)
    {
        using var image = Image.Load(file.OpenReadStream());
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
                Size = new Size(targetWidth, targetHeight),
                Mode = ResizeMode.Crop
            });
        });

        var stream = new MemoryStream();
        image.SaveAsWebpAsync(stream);
        return stream;
    }
}