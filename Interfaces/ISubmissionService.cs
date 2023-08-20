using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface ISubmissionService
{
    Task<Song> ApproveSong(SongSubmission songSubmission, bool isOriginal, bool isHidden);

    Task RejectSong(SongSubmission songSubmission);

    Task ApproveChart(ChartSubmission chartSubmission);

    Task RejectChart(ChartSubmission chartSubmission);
}