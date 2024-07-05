namespace PhiZoneApi.Models;

public class Like : OwnedResource
{
    public Guid ResourceId { get; set; }

    public LikeableResource Resource { get; set; } = null!;
}