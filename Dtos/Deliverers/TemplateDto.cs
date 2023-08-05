using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Deliverers;

public class TemplateDto
{
    public List<Email> Emails { get; set; } = null!;

    public List<Message> Messages { get; set; } = null!;
}

public class Email
{
    public EmailRequestMode Mode { get; set; }

    public string Subject { get; set; } = null!;

    public string Body { get; set; } = null!;
}

public class Message
{
    public string Key { get; set; } = null!;

    public string Content { get; set; } = null!;
}