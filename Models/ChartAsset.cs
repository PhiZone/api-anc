using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class ChartAsset : Resource
{
    public Guid ChartId { get; set; }

    public Chart Chart { get; set; } = null!;

    public ChartAssetType Type { get; set; }

    public string Name { get; set; } = null!;

    public string File { get; set; } = null!;

    public DateTimeOffset DateUpdated { get; set; }
}