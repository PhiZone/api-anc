namespace PhiZoneApi.Dtos.Responses;

public class HostshipDetailedDto : HostshipDto
{
    public bool IsUnveiled { get; set; }
    
    public List<uint> Permissions { get; set; } = [];
}