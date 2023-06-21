namespace PhiZoneApi.Dtos;

public class MailDto
{
    public required string RecipientAddress { get; init; }

    public required string RecipientName { get; init; }

    public required string EmailSubject { get; init; }

    public required string EmailBody { get; init; }
}