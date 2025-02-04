namespace PhiZoneApi.Dtos.Responses;

public class SubmissionSongDto
{
    public IEnumerable<SongMatchDto> SongMatches { get; set; } = [];

    public IEnumerable<SongSubmissionMatchDto> SongSubmissionMatches { get; set; } = [];

    public IEnumerable<ResourceRecordMatchDto> ResourceRecordMatches { get; set; } = [];
}