using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class Admission
{
    public Guid AdmitterId { get; set; }

    public Guid AdmitteeId { get; set; }

    public PublicResource Admitter { get; set; } = null!;

    public PublicResource Admittee { get; set; } = null!;
    
    public RequestStatus Status { get; set; }
    
    public string? Label { get; set; }
    
    public int RequesterId { get; set; }
    
    public User Requester { get; set; } = null!;

    public int RequesteeId { get; set; }
    
    public User Requestee { get; set; } = null!;

    public DateTimeOffset DateCreated { get; set; }
}