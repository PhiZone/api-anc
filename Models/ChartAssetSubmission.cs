using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class ChartAssetSubmission : Resource
{
    public Guid ChartSubmissionId { get; set; }

    public ChartSubmission ChartSubmission { get; set; } = null!;

    public ChartAssetType Type { get; set; }

    public string Name { get; set; } = null!;

    public string File { get; set; } = null!;

    public DateTimeOffset DateUpdated { get; set; }
}