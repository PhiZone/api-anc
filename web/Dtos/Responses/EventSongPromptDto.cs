namespace PhiZoneApi.Dtos.Responses;

public class EventSongPromptDto : SongDto
{
    public string? Label { get; set; }

    public string? EventDescription { get; set; }
}