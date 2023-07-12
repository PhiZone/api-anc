using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class PlayConfigurationResponseDto
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public int PerfectJudgment { get; set; }

    public int GoodJudgment { get; set; }

    public List<int> AspectRatio { get; set; } = new(2);

    public double NoteSize { get; set; }

    public ChartMirroringMode ChartMirroring { get; set; }

    public double BackgroundLuminance { get; set; }

    public double BackgroundBlur { get; set; }

    public bool SimultaneousNoteHint { get; set; }

    public bool FcApIndicator { get; set; }

    public int ChartOffset { get; set; }

    public double HitSoundVolume { get; set; }

    public double MusicVolume { get; set; }

    public int OwnerId { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}