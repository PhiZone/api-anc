using PhiZoneApi.Dtos.Responses;

namespace PhiZoneApi.Dtos.Deliverers;

public class PhigrimInheritanceDto
{
    public IEnumerable<TapTapLink> Links = [];
    public UserDto User { get; set; } = null!;
}

public class TapTapLink
{
    public Guid ApplicationId { get; set; }

    public string TapUnionId { get; set; } = null!;
}