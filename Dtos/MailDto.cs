namespace PhiZoneApi.Dtos;

public class MailDto
{
    public required string RecipientAddress { get; set; }
    public required string RecipientName { get; set; }
    public required string EmailSubject { get; set; }
    public required string EmailBody { get; set; }
}