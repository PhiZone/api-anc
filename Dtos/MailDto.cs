namespace PhiZoneApi.Dtos;

public class MailDto
{
    public string RecipientAddress { get; init; } = string.Empty;

    public string RecipientName { get; init; } = string.Empty;

    public string EmailSubject { get; init; } = string.Empty;

    public string EmailBody { get; init; } = string.Empty;
}