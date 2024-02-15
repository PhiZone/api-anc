namespace PhiZoneApi.Models;

public class Vote : Resource
{
    public Guid ChartId { get; set; }

    public Chart Chart { get; set; } = null!;

    public int Arrangement { get; set; }

    public int Gameplay { get; set; }

    public int VisualEffects { get; set; }

    public int Creativity { get; set; }

    public int Concord { get; set; }

    public int Impression { get; set; }

    public int Total { get; set; }

    public double Multiplier { get; set; }
}