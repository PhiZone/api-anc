using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Deliverers;

public class MailTaskDto
{
    public User User { get; init; } = null!;

    public string EmailSubject { get; init; } = null!;

    public string EmailBody { get; init; } = null!;

    public SucceedingAction? SucceedingAction { get; init; }
}