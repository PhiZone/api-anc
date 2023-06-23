namespace PhiZoneApi.Configurations;

/// <summary>
///     Stores settings for Mail Service.
/// </summary>
public class MailSettings
{
    /// <summary>
    ///     Stores the SMTP server host to connect to.
    /// </summary>
    public string Server { get; set; } = null!;

    /// <summary>
    ///     Stores the port number to connect to.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    ///     Determines the name of the sender.
    /// </summary>
    public string SenderName { get; set; } = null!;

    /// <summary>
    ///     Determines the address of the sender.
    /// </summary>
    public string SenderAddress { get; set; } = null!;

    /// <summary>
    ///     Stores the email address of the sender.
    /// </summary>
    public string UserName { get; set; } = null!;

    /// <summary>
    ///     Stores the password of the sender.
    /// </summary>
    public string Password { get; set; } = null!;
}