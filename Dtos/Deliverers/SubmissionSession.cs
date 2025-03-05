using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Deliverers;

public class SubmissionSession
{
    public Guid Id { get; set; }

    public SubmissionSessionStatus Status { get; set; }

    public string? SongPath { get; set; }

    public string? IllustrationPath { get; set; }

    public SongResults? SongResults { get; set; }

    public ChartSubmission? Chart { get; set; }

    public List<SessionChartAsset> Assets { get; set; } = [];
}

public class SongResults
{
    public IEnumerable<SeekTuneFindResult> SongMatches { get; set; } = [];

    public IEnumerable<SeekTuneFindResult> ResourceRecordMatches { get; set; } = [];
}

public class SessionChartAsset
{
    public ChartAssetType Type { get; set; }

    public string Name { get; set; } = null!;

    public string File { get; set; } = null!;
}