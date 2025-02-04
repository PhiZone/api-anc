using PhiZoneApi.Dtos.Deliverers;

namespace PhiZoneApi.Interfaces;

public interface ISeekTuneService
{
    Task<List<SeekTuneFindResult>?> FindMatches(string pathToSong, bool resourceRecords = false);
}