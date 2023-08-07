namespace PhiZoneApi.Interfaces;

public interface IMultimediaService
{
    MemoryStream CropImage(IFormFile file, (int, int) aspectRatio);

    MemoryStream CropImage(byte[] buffer, (int, int) aspectRatio);

    Task<MemoryStream?> ConvertAudio(IFormFile file);

    Task<MemoryStream?> ConvertAudio(byte[] buffer);
}