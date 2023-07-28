using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Requests;

public class AdmissionRequestDto
{
    public Guid AdmitterId { get; set; }

    public Guid AdmitteeId { get; set; }
    
    public RequestStatus Status { get; set; }
    
    public string? Label { get; set; }
}