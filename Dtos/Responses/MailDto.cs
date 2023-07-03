using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Responses;

public class MailDto
{
    public User User { get; init; } = null!;

    public string EmailSubject { get; init; } = null!;

    public string EmailBody { get; init; } = null!;

    public SucceedingAction? SucceedingAction { get; init; }
}