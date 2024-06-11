using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class EventDivisionUpdateDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(40, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Title { get; set; } = null!;

    [MaxLength(80, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Subtitle { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 2, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public EventDivisionType Type { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 3, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public EventDivisionStatus Status { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(200, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Illustrator { get; set; } = null!;

    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Description { get; set; }

    public Guid? TagId { get; set; }

    public int? MinTeamCount { get; set; }

    public int? MaxTeamCount { get; set; }

    public int? MinParticipantPerTeamCount { get; set; }

    public int? MaxParticipantPerTeamCount { get; set; }

    public int? MinSubmissionCount { get; set; }

    public int? MaxSubmissionCount { get; set; }
    
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool Anonymization { get; set; }
    
    public List<string?> Preserved { get; set; } = [];

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 2, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public Accessibility Accessibility { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool IsHidden { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool IsLocked { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public Guid EventId { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int OwnerId { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public DateTimeOffset DateUnveiled { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public DateTimeOffset DateStarted { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public DateTimeOffset DateEnded { get; set; }
}