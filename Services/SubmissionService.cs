using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Services;

public class SubmissionService : ISubmissionService
{
    private readonly ISongRepository _songRepository;
    private readonly INotificationService _notificationService;
    
    public SubmissionService(ISongRepository songRepository, INotificationService notificationService)
    {
        _songRepository = songRepository;
        _notificationService = notificationService;
    }

    public async Task<Song> ApproveSong(SongSubmission songSubmission, bool isOriginal)
    {
        var song = new Song
        {
            Title = songSubmission.Title,
            EditionType = songSubmission.EditionType,
            Edition = songSubmission.Edition,
            AuthorName = songSubmission.AuthorName,
            File = songSubmission.File,
            FileChecksum = songSubmission.FileChecksum,
            Illustration = songSubmission.Illustration,
            Illustrator = songSubmission.Illustrator,
            Description = songSubmission.Description,
            Accessibility = songSubmission.Accessibility,
            IsHidden = false,
            IsLocked = false,
            Lyrics = songSubmission.Lyrics,
            Bpm = songSubmission.Bpm,
            MinBpm = songSubmission.MinBpm,
            MaxBpm = songSubmission.MaxBpm,
            Offset = songSubmission.Offset,
            IsOriginal = isOriginal,
            Duration = songSubmission.Duration,
            PreviewStart = songSubmission.PreviewStart,
            PreviewEnd = songSubmission.PreviewEnd,
            OwnerId = songSubmission.OwnerId,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };
        await _songRepository.CreateSongAsync(song);
        await _notificationService.Notify(songSubmission.Owner, songSubmission.Reviewer, NotificationType.Requests,
            "song-submission-approval", new Dictionary<string, string>
            {
                {"Song", ResourceUtil.GetRichText<SongSubmission>(songSubmission.Id.ToString(), songSubmission.GetDisplay())}
            });
        return song;
    }

    public async Task RejectSong(SongSubmission songSubmission)
    {
        await _notificationService.Notify(songSubmission.Owner, songSubmission.Reviewer, NotificationType.Requests,
            "song-submission-rejection", new Dictionary<string, string>
            {
                {"Song", ResourceUtil.GetRichText<SongSubmission>(songSubmission.Id.ToString(), songSubmission.GetDisplay())},
                {"Reason", songSubmission.Message!}
            });
    }
}