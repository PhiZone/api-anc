using PhiZoneApi.Data;
using PhiZoneApi.Dtos.Deliverers;

namespace PhiZoneApi.Interfaces;

public interface ISeekTuneService
{
    Task InitializeAsync(ApplicationDbContext context, CancellationToken cancellationToken);

    Task<List<SeekTuneFindResult>?> FindMatches(string pathToSong, bool resourceRecords = false, int take = -1);

    Task<bool> CreateFingerprint(Guid id, string title, string? version, string artist, string songLocation,
        bool isUrl = false,
        bool resourceRecords = false);

    Task<bool> CheckIfExists(Guid id, bool resourceRecords = false);
}