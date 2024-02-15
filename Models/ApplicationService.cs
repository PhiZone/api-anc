using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class ApplicationService
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public ServiceTargetType TargetType { get; set; }

    public string? Description { get; set; }

    public string Code { get; set; } = null!;

    public List<string> Parameters { get; set; } = [];

    public Guid ApplicationId { get; set; }

    public Application Application { get; set; } = null!;

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUpdated { get; set; }
}