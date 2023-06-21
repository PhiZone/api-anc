namespace PhiZoneApi.Models;

public class Region
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Flag { get; set; }

    public ICollection<User>? Users { get; set; }
}