using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class ChartAssetDto
{
    public Guid Id { get; set; }

    public int? OwnerId { get; set; }

    public Guid ChartId { get; set; }

    public ChartAssetType Type { get; set; }

    public string Name { get; set; } = null!;

    public string File { get; set; } = null!;

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUpdated { get; set; }
}