using NVorbis;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Services;

public class SongService : ISongService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ISongRepository _songRepository;

    public SongService(IFileStorageService fileStorageService, IRabbitMqService rabbitMqService,
        ISongRepository songRepository)
    {
        _fileStorageService = fileStorageService;
        _rabbitMqService = rabbitMqService;
        _songRepository = songRepository;
    }

    public async Task<(string, TimeSpan)?> UploadAsync(string fileName, IFormFile file)
    {
        var stream = await MultimediaUtil.ConvertAudio(file);
        if (stream == null) return null;
        using var vorbis = new VorbisReader(stream, false);
        var duration = vorbis.TotalTime;
        var url = await _fileStorageService.Upload<Song>(fileName, stream, "ogg");
        return (url, duration);
    }

    public async Task<(string, TimeSpan)?> UploadAsync(string fileName, byte[] buffer)
    {
        var stream = await MultimediaUtil.ConvertAudio(buffer);
        if (stream == null) return null;
        using var vorbis = new VorbisReader(stream, false);
        var duration = vorbis.TotalTime;
        var url = await _fileStorageService.Upload<Song>(fileName, stream, "ogg");
        return (url, duration);
    }

    public async Task PublishAsync(IFormFile file, Guid songId)
    {
        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        using var channel = _rabbitMqService.GetConnection().CreateModel();
        var properties = channel.CreateBasicProperties();
        properties.Headers = new Dictionary<string, object>
        {
            { "SongId", songId.ToString() }
        };
        channel.BasicPublish("", "song", false, properties, memoryStream.ToArray());
    }

    public async Task<bool> UpdateSongAsync(Song song, (string, TimeSpan) songInfo)
    {
        song.File = songInfo.Item1;
        song.Duration = songInfo.Item2;
        return await _songRepository.UpdateSongAsync(song);
    }
}