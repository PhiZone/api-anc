namespace PhiZoneApi.Models;

public class Collection : PublicResource
{
    public string Title { get; set; } = null!;

    public string Subtitle { get; set; } = null!;

    public string Illustration { get; set; } = null!;

    public string Illustrator { get; set; } = null!;

    public override string GetDisplay()
    {
        return $"{Title} - {Subtitle}";
    }
}