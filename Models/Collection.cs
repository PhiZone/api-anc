namespace PhiZoneApi.Models;

public class Collection : PublicResource
{
    public string Title { get; set; } = null!;

    public string Subtitle { get; set; } = null!;

    public string Illustration { get; set; } = null!;

    public string Illustrator { get; set; } = null!;

    public IEnumerable<Chart> Charts { get; } = new List<Chart>();

    public IEnumerable<Admission> ChartAdmittees { get; } = new List<Admission>();

    public override string GetDisplay()
    {
        return $"{Title} - {Subtitle}";
    }
}