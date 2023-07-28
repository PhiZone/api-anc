namespace PhiZoneApi.Models;

public class Chapter : PublicResource
{
    public string Title { get; set; } = null!;

    public string Subtitle { get; set; } = null!;

    public string Illustration { get; set; } = null!;

    public string Illustrator { get; set; } = null!;

    public IEnumerable<Song> Songs { get; } = new List<Song>();

    public IEnumerable<Admission> SongAdmittees { get; } = new List<Admission>();
}