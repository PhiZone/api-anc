using PhiZoneApi.Data;
using PhiZoneApi.Dtos.Deliverers;

namespace PhiZoneApi.Interfaces;

public interface ISeekTuneService
{
    Task InitializeAsync(ApplicationDbContext context, CancellationToken cancellationToken);

    Task<List<SeekTuneFindResult>?> FindMatches(string pathToSong, bool resourceRecords = false);

    Task<bool> CreateFingerprint(Guid id, string title, string artist, string songLocation, bool isUrl = false,
        bool resourceRecords = false);

    Task<bool> CheckIfExists(Guid id, bool resourceRecords = false);
}