using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public abstract class Submission : Resource
{
    public string? Description { get; set; }

    public Accessibility Accessibility { get; set; }

    public RequestStatus Status { get; set; }

    public Guid? RepresentationId { get; set; }

    public PublicResource? Representation { get; set; }

    public List<string> Tags { get; set; } = [];

    public DateTimeOffset DateUpdated { get; set; }
}