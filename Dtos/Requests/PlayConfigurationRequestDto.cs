using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class PlayConfigurationRequestDto
{
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Name { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int PerfectJudgment { get; set; } = 80;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int GoodJudgment { get; set; } = 160;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public ChartMirroringMode ChartMirroring { get; set; } = ChartMirroringMode.Off;

    public List<int>? AspectRatio { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0.4, 2, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public double NoteSize { get; set; } = 1;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 1, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public double BackgroundLuminance { get; set; } = 0.5;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 2, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public double BackgroundBlur { get; set; } = 1;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool SimultaneousNoteHint { get; set; } = true;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool FcApIndicator { get; set; } = true;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(-600, 600, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public int ChartOffset { get; set; } = 0;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 1, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public double HitSoundVolume { get; set; } = 1;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 1, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public double MusicVolume { get; set; } = 1;
}