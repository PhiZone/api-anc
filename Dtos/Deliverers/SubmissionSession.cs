using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Deliverers;

public class SubmissionSession
{
    public int UserId { get; set; }

    public SubmissionSessionStatus Status { get; set; }

    public string? SongPath { get; set; }

    public string? IllustrationPath { get; set; }

    public SongResults? SongResults { get; set; }

    public string? Chart { get; set; }

    public IEnumerable<Asset> Assets { get; set; } = [];
}

public class SongResults
{
    public IEnumerable<SeekTuneFindResult> SongMatches { get; set; } = [];

    public IEnumerable<SeekTuneFindResult> ResourceRecordMatches { get; set; } = [];
}

public class Asset
{
    public ChartAssetType Type { get; set; }

    public string Name { get; set; } = null!;

    public string File { get; set; } = null!;
}