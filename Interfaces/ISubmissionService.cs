using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface ISubmissionService
{
    Task<Song> ApproveSong(SongSubmission songSubmission, bool isOriginal, bool isHidden, bool isLocked);

    Task RejectSong(SongSubmission songSubmission);

    Task ApproveChart(ChartSubmission chartSubmission, Guid? songId = null, bool? isRanked = null);

    Task RejectChart(ChartSubmission chartSubmission);
}