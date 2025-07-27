using System.Text.Json.Serialization;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class ServiceScript : Resource
{
    public string Name { get; set; } = null!;

    public ServiceTargetType TargetType { get; set; }

    public string? Description { get; set; }

    public string Code { get; set; } = null!;

    public List<string> Parameters { get; set; } = [];

    public Guid? ResourceId { get; set; }

    [JsonIgnore] public LikeableResource? Resource { get; set; }

    public DateTimeOffset DateUpdated { get; set; }
}