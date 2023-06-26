namespace PhiZoneApi.Configurations;

/// <summary>
///     Stores secrets and settings for RabbitMQ.
/// </summary>
public class RabbitMqSettings
{
    /// <summary>
    ///     Stores a host name.
    /// </summary>
    public string HostName { get; set; } = null!;

    /// <summary>
    ///     Stores a port.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    ///     Stores a user name.
    /// </summary>
    public string UserName { get; set; } = null!;

    /// <summary>
    ///     Stores a password.
    /// </summary>
    public string Password { get; set; } = null!;
}