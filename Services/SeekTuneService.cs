using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class SeekTuneService(IConfiguration config) : ISeekTuneService
{
    private readonly string _seekTuneApiUrl = config["SeekTuneApiUrl"]!;
    private readonly HttpClient _client = new() { BaseAddress = new Uri(config["SeekTuneApiUrl"]!) };

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
            Console.WriteLine("Failed to find matches with seek-tune with status code " + response.StatusCode);
            return null;
        }

        var matches = JsonConvert.DeserializeObject<List<SeekTuneFindResult>>(content);
        return matches;
    }
}