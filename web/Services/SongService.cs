using NVorbis;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

// using RabbitMQ.Client;

namespace PhiZoneApi.Services;

public class SongService(
    IFileStorageService fileStorageService,
    // IRabbitMqService rabbitMqService,
    INatsService natsService,
    IMultimediaService multimediaService,
    ILogger<SongService> logger,
    IHostEnvironment env) : ISongService
{
    private readonly string _queue = env.IsProduction() ? "song" : "song-dev";

    public async Task<(string, string, TimeSpan)?> UploadAsync(string fileName, IFormFile file)
    {
        var stream = await multimediaService.ConvertAudio(file);
        if (stream == null) return null;
        try
        {
            var result = await fileStorageService.Upload<Song>(fileName, stream, "ogg");
            using var vorbis = new VorbisReader(stream, false);
            var duration = vorbis.TotalTime;
            return (result.Item1, result.Item2, duration);
        }
        catch (Exception e)
        {
            logger.LogWarning(LogEvents.AudioFailure, e, "Failed to read audio from {File}", fileName);
            return null;
        }
    }

    public async Task<(string, string, TimeSpan)?> UploadAsync(string fileName, byte[] buffer)
    {
        var stream = await multimediaService.ConvertAudio(buffer);
        if (stream == null) return null;
        try
        {
            var result = await fileStorageService.Upload<Song>(fileName, stream, "ogg");
            using var vorbis = new VorbisReader(stream, false);
            var duration = vorbis.TotalTime;
            return (result.Item1, result.Item2, duration);
        }
        catch (Exception e)
        {
            logger.LogWarning(LogEvents.AudioFailure, e, "Failed to read audio from {File}", fileName);
            return null;
        }
    }

    public async Task PublishAsync(IFormFile file, Guid songId, bool isSubmission = false, bool burn = true)
    {
        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        // await using var channel = await rabbitMqService.GetClient().CreateChannelAsync();
        // var properties = new BasicProperties
        // {
        //     Headers = new Dictionary<string, object?>
        //     {
        //         { "SongId", songId.ToString() },
        //         { "IsSubmission", isSubmission.ToString() },
        //         { "Burn", burn.ToString() }
        //     }
        // };
        // await channel.BasicPublishAsync("", _queue, false, properties, memoryStream.ToArray());
        await natsService.GetClient()
            .PublishAsync(_queue,
                new SongTaskDto
                {
                    SongId = songId, IsSubmission = isSubmission, Burn = burn, Body = memoryStream.ToArray()
                });
    }
}