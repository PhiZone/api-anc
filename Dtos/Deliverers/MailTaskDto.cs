namespace PhiZoneApi.Dtos.Deliverers;

public class MailTaskDto
{
    public string UserName { get; set; } = null!;

    public string EmailAddress { get; set; } = null!;

    public string EmailSubject { get; init; } = null!;

    public string EmailBody { get; init; } = null!;
}