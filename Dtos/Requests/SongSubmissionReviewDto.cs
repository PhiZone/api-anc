using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Requests;

public class SongSubmissionReviewDto
{
    public RequestStatus Status { get; set; }
    
    public bool IsOriginal { get; set; }
    
    public string? Message { get; set; }
}