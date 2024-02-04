namespace PhiZoneApi.Dtos.Responses;

public class TagDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string NormalizedName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}