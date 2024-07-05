using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class ResourceRecord : Resource
{
    public ResourceRecordType Type { get; set; }

    public string Title { get; set; } = null!;

    public EditionType EditionType { get; set; }

    public string? Edition { get; set; }

    public string AuthorName { get; set; } = null!;

    public string? Description { get; set; }

    public ResourceRecordStrategy Strategy { get; set; }

    public string Source { get; set; } = null!;

    public string CopyrightOwner { get; set; } = null!;

    public DateTimeOffset DateUpdated { get; set; }
}