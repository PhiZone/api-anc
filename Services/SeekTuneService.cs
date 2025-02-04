using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PhiZoneApi.Data;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class SeekTuneService(IConfiguration config) : ISeekTuneService
{
    private readonly HttpClient _client = new() { BaseAddress = new Uri(config["SeekTuneApiUrl"]!) };
    private readonly string _seekTuneApiUrl = config["SeekTuneApiUrl"]!;

    public async Task InitializeAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var songIds = new List<Guid>();
        foreach (var songSubmission in await context.SongSubmissions.Where(e => e.File != null)
                     .OrderBy(e => e.DateCreated)
                     .ToListAsync(cancellationToken))
        {
            await CreateFingerprint(songSubmission.Id, songSubmission.Title, songSubmission.AuthorName,
                songSubmission.File!, true);
            if (songSubmission.RepresentationId != null) songIds.Add(songSubmission.RepresentationId.Value);
        }

        foreach (var song in await context.Songs.Where(e => e.File != null && !songIds.Contains(e.Id))
                     .OrderBy(e => e.DateCreated)
                     .ToListAsync(cancellationToken))
            await CreateFingerprint(song.Id, song.Title, song.AuthorName, song.File!, true);

        foreach (var resourceRecord in await context.ResourceRecords.Where(e => e.Media != null)
                     .ToListAsync(cancellationToken))
            await CreateFingerprint(resourceRecord.Id, resourceRecord.Title, resourceRecord.AuthorName,
                resourceRecord.Media!, true, true);
    }

    public async Task<List<SeekTuneFindResult>?> FindMatches(string pathToSong, bool resourceRecords = false)
    {
        var route = resourceRecords ? "resourceRecords" : "songs";
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new UriBuilder($"{_seekTuneApiUrl}/{route}/find")
            {
                Query = new QueryBuilder { { "songPath", pathToSong } }.ToString()
            }.Uri
        };
        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("Failed to find matches with SeekTune with status code " + response.StatusCode);
            return null;
        }

        var matches = JsonConvert.DeserializeObject<List<SeekTuneFindResult>>(content);
        return matches;
    }

    public async Task<bool> CreateFingerprint(Guid id, string title, string artist, string songLocation,
        bool isUrl = false, bool resourceRecords = false)
    {
        if (await CheckIfExists(id, resourceRecords)) return false;
        var route = resourceRecords ? "resourceRecords" : "songs";
        var data = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { isUrl ? "songUrl" : "songPath", songLocation },
            { "title", title },
            { "artist", artist },
            { "pzID", id.ToString() }
        });
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post, RequestUri = new Uri($"{_seekTuneApiUrl}/{route}/create"), Content = data
        };
        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CheckIfExists(Guid id, bool resourceRecords = false)
    {
        var route = resourceRecords ? "resourceRecords" : "songs";
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new UriBuilder($"{_seekTuneApiUrl}/{route}/checkExists")
            {
                Query = new QueryBuilder { { "pzID", id.ToString() } }.ToString()
            }.Uri
        };
        var response = await _client.SendAsync(request);
        return response.IsSuccessStatusCode;
    }
}