using NVorbis;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class SongService : ISongService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IMultimediaService _multimediaService;
    private readonly IRabbitMqService _rabbitMqService;

    public SongService(IFileStorageService fileStorageService, IRabbitMqService rabbitMqService,
        IMultimediaService multimediaService)
    {
        _fileStorageService = fileStorageService;
        _rabbitMqService = rabbitMqService;
        _multimediaService = multimediaService;
    }

    public async Task<(string, string, TimeSpan)?> UploadAsync(string fileName, IFormFile file)
    {
        var stream = await _multimediaService.ConvertAudio(file);
        if (stream == null) return null;
        using var vorbis = new VorbisReader(stream, false);
        var duration = vorbis.TotalTime;
        var result = await _fileStorageService.Upload<Song>(fileName, stream, "ogg");
        return (result.Item1, result.Item2, duration);
    }

    public async Task<(string, string, TimeSpan)?> UploadAsync(string fileName, byte[] buffer)
    {
        var stream = await _multimediaService.ConvertAudio(buffer);
        if (stream == null) return null;
        using var vorbis = new VorbisReader(stream, false);
        var duration = vorbis.TotalTime;
        var result = await _fileStorageService.Upload<Song>(fileName, stream, "ogg");
        return (result.Item1, result.Item2, duration);
    }

    public async Task PublishAsync(IFormFile file, Guid songId, bool isSubmission = false)
    {
        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        using var channel = _rabbitMqService.GetConnection().CreateModel();
        var properties = channel.CreateBasicProperties();
        properties.Headers = new Dictionary<string, object>
        {
            { "SongId", songId.ToString() },
            { "IsSubmission", isSubmission.ToString() }
        };
        channel.BasicPublish("", "song", false, properties, memoryStream.ToArray());
    }
}