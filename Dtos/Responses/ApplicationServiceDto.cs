using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class ApplicationServiceDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public ServiceTargetType TargetType { get; set; }

    public string? Description { get; set; }

    public string Code { get; set; } = null!;

    public List<string> Parameters { get; set; } = [];

    public Guid ApplicationId { get; set; }

    public ApplicationDto Application { get; set; } = null!;

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUpdated { get; set; }
}