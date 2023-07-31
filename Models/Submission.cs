using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class Submission : Resource
{
    public string? Description { get; set; }

    public Accessibility Accessibility { get; set; }

    public RequestStatus Status { get; set; }

    public RequestStatus VolunteerStatus { get; set; }

    public RequestStatus CollabStatus { get; set; }

    public Guid? RepresentationId { get; set; }

    public PublicResource? Representation { get; set; }

    public DateTimeOffset DateUpdated { get; set; }
}