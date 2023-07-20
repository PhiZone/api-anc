using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class Submission : Resource
{
    public string? Description { get; set; }

    public Accessibility Accessibility { get; set; }
    
    public SubmissionStatus Status { get; set; }
    
    public SubmissionStatus VolunteerStatus { get; set; }
    
    public SubmissionStatus CollabStatus { get; set; }
    
    public SubmissionStatus AdmissionStatus { get; set; }
    
    public Guid? RepresentationId { get; set; }
    
    public PublicResource? Representation { get; set; }
    
    public DateTimeOffset DateUpdated { get; set; }
}