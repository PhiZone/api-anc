using System.Net;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PhiZoneApi.Constants;
using PhiZoneApi.Data;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class SeekTuneService(IConfiguration config, ILogger<SeekTuneService> logger) : ISeekTuneService
{
    private readonly HttpClient _client = new()
    {
        BaseAddress = new Uri(config["SeekTuneUrl"]!), Timeout = TimeSpan.FromMinutes(15)
    };

    private readonly string _seekTuneUrl = config["SeekTuneUrl"]!;

    public async Task InitializeAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var songIds = new List<Guid>();
        foreach (var songSubmission in await context.SongSubmissions
                     .Where(e => e.File != null && e.Status != RequestStatus.Rejected)
                     .OrderBy(e => e.DateCreated)
                     .ToListAsync(cancellationToken))
        {
            await CreateFingerprint(songSubmission.Id, songSubmission.Title, songSubmission.Edition,
                songSubmission.AuthorName, songSubmission.File!, true);
            if (songSubmission.RepresentationId != null) songIds.Add(songSubmission.RepresentationId.Value);
        }

        foreach (var song in await context.Songs.Where(e => e.File != null && !songIds.Contains(e.Id))
                     .OrderBy(e => e.DateCreated)
                     .ToListAsync(cancellationToken))
            await CreateFingerprint(song.Id, song.Title, song.Edition, song.AuthorName, song.File!, true);

        foreach (var resourceRecord in await context.ResourceRecords.Where(e => e.Media != null)
                     .ToListAsync(cancellationToken))
            await CreateFingerprint(resourceRecord.Id, resourceRecord.Title, resourceRecord.Edition,
                resourceRecord.AuthorName, resourceRecord.Media!, true, true);
    }

    public async Task<List<SeekTuneFindResult>?> FindMatches(string pathToSong, bool resourceRecords = false,
        int take = -1)
    {
        var route = resourceRecords ? "resourceRecords" : "songs";
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new UriBuilder($"{_seekTuneUrl}/{route}/find")
            {
                Query = new QueryBuilder { { "songPath", pathToSong } }.ToString()
            }.Uri
        };
        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        logger.LogInformation(LogEvents.SeekTuneInfo, "Match find response ({Status}): {Content}", response.StatusCode,
            content);

        if (response.StatusCode == HttpStatusCode.NotFound) return [];

        if (!response.IsSuccessStatusCode) return null;

        var matches =
            JsonConvert.DeserializeObject<IEnumerable<SeekTuneFindResult>>(content)!.OrderByDescending(e => e.Score);
        return (take >= 0 ? matches.Take(take) : matches).ToList();
    }

    public async Task<bool> CreateFingerprint(Guid id, string title, string? version, string artist,
        string songLocation, bool isUrl = false, bool resourceRecords = false)
    {
        if (await CheckIfExists(id, resourceRecords)) return false;
        var route = resourceRecords ? "resourceRecords" : "songs";
        var data = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { isUrl ? "songUrl" : "songPath", songLocation },
            { "title", $"{title}{(version != null ? $" ({version})" : "")}" },
            { "artist", artist },
            { "uuid", id.ToString() }
        });
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post, RequestUri = new Uri($"{_seekTuneUrl}/{route}/create"), Content = data
        };
        try
        {
            var response = await _client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation(LogEvents.SeekTuneInfo, "Fingerprint creation response ({Status}): {Content}",
                response.StatusCode, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            logger.LogError(LogEvents.SeekTuneFailure, e, "Failed to create fingerprint for {Id}", id);
            return false;
        }
    }

    public async Task<bool> CheckIfExists(Guid id, bool resourceRecords = false)
    {
        var route = resourceRecords ? "resourceRecords" : "songs";
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new UriBuilder($"{_seekTuneUrl}/{route}/checkExists")
            {
                Query = new QueryBuilder { { "uuid", id.ToString() } }.ToString()
            }.Uri
        };
        var response = await _client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }
}