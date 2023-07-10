namespace PhiZoneApi.Models;

public class Like : Resource
{
    public Guid ResourceId { get; set; }

    public LikeableResource Resource { get; set; } = null!;
}