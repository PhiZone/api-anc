namespace PhiZoneApi.Models;

public class Authorship
{
    public Guid Id { get; set; }

    public Guid ResourceId { get; set; }

    public SignificantResource Resource { get; set; } = null!;

    public int AuthorId { get; set; }

    public User Author { get; set; } = null!;

    public string? Position { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}