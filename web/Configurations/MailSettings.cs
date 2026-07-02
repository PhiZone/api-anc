namespace PhiZoneApi.Configurations;

/// <summary>
///     Stores SMTP settings for email delivery.
/// </summary>
public class MailSettings
{
    /// <summary>
    ///     Stores the SMTP server host name.
    /// </summary>
    public string Server { get; set; } = null!;

    /// <summary>
    ///     Stores the SMTP server port.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    ///     Stores the display name of the sender.
    /// </summary>
    public string SenderName { get; set; } = null!;

    /// <summary>
    ///     Stores the sender email address.
    /// </summary>
    public string SenderAddress { get; set; } = null!;

    /// <summary>
    ///     Stores the SMTP user name.
    /// </summary>
    public string UserName { get; set; } = null!;

    /// <summary>
    ///     Stores the SMTP password.
    /// </summary>
    public string Password { get; set; } = null!;
}
