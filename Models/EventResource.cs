using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

[PrimaryKey(nameof(DivisionId), nameof(ResourceId))]
public class EventResource
{
    public Guid DivisionId { get; set; }

    public EventDivision Division { get; set; } = null!;

    public Guid ResourceId { get; set; }

    public LikeableResource Resource { get; set; } = null!;

    public EventResourceType Type { get; set; }

    public string? Label { get; set; }

    public string? Description { get; set; }

    public bool? IsAnonymous { get; set; }

    public double? Score { get; set; }

    [JsonIgnore] public List<string?> Preserved { get; set; } = [];

    public Guid? TeamId { get; set; }

    public EventTeam? Team { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}