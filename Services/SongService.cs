using NVorbis;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class SongService(IFileStorageService fileStorageService, IRabbitMqService rabbitMqService,
        IMultimediaService multimediaService)
    : ISongService
{
    public async Task<(string, string, TimeSpan)?> UploadAsync(string fileName, IFormFile file)
    {
        var stream = await multimediaService.ConvertAudio(file);
        if (stream == null) return null;
        using var vorbis = new VorbisReader(stream, false);
        var duration = vorbis.TotalTime;
        var result = await fileStorageService.Upload<Song>(fileName, stream, "ogg");
        return (result.Item1, result.Item2, duration);
    }

    public async Task<(string, string, TimeSpan)?> UploadAsync(string fileName, byte[] buffer)
    {
        var stream = await multimediaService.ConvertAudio(buffer);
        if (stream == null) return null;
        using var vorbis = new VorbisReader(stream, false);
        var duration = vorbis.TotalTime;
        var result = await fileStorageService.Upload<Song>(fileName, stream, "ogg");
        return (result.Item1, result.Item2, duration);
    }

    public async Task PublishAsync(IFormFile file, Guid songId, bool isSubmission = false)
    {
        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        using var channel = rabbitMqService.GetConnection().CreateModel();
        var properties = channel.CreateBasicProperties();
        properties.Headers = new Dictionary<string, object>
        {
            { "SongId", songId.ToString() },
            { "IsSubmission", isSubmission.ToString() }
        };
        channel.BasicPublish("", "song", false, properties, memoryStream.ToArray());
    }
}