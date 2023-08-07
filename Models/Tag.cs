namespace PhiZoneApi.Models;

public class Tag
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string NormalizedName { get; set; } = null!;

    public string? Description { get; set; }

    public List<PublicResource> Resources { get; set; } = new();

    public DateTimeOffset DateCreated { get; set; }
}