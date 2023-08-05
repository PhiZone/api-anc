using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Requests;

public class PlayConfigurationRequestDto
{
    public string? Name { get; set; }

    public int PerfectJudgment { get; set; } = 80;

    public int GoodJudgment { get; set; } = 160;

    public List<int>? AspectRatio { get; set; }

    [Range(0.4, 2, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public double NoteSize { get; set; } = 1;

    public ChartMirroringMode ChartMirroring { get; set; } = ChartMirroringMode.Off;

    [Range(0, 1, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public double BackgroundLuminance { get; set; } = 0.5;

    [Range(0, 2, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public double BackgroundBlur { get; set; } = 1;

    public bool SimultaneousNoteHint { get; set; } = true;

    public bool FcApIndicator { get; set; } = true;

    [Range(-600, 600, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public int ChartOffset { get; set; } = 0;

    [Range(0, 1, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public double HitSoundVolume { get; set; } = 1;

    [Range(0, 1, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public double MusicVolume { get; set; } = 1;
}