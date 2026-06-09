namespace Wup.Works.SiteBroker.Client.Configuration;

/// <summary>
/// Configuration model for the MQTT client.
/// </summary>
public sealed class MqttOptions
{
    /// <summary>
    /// Time for an automatic reconnect.
    /// </summary>
    public TimeSpan AutoReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// If the session should be cleared on reconnect.
    /// If set to false, e.g. subscriptions get preserved and reapplied on reconnect of the client.
    /// </summary>
    public bool CleanSession { get; set; } = true;

    /// <summary>
    /// Connecting client timeout in seconds.
    /// </summary>
    public int ConnectTimeoutInSeconds { get; set; } = 90;

    /// <summary>
    /// Hostname of the broker.
    /// </summary>
    public string Hostname { get; set; }

    /// <summary>
    /// Id of the mqtt client. If not specified, a random guid will get set.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Number of messages saved within the message queue to publish.
    /// </summary>
    public int MaxPendingMessages { get; set; } = ushort.MaxValue;

    /// <summary>
    /// Password of the broker.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Port of the broker.
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// Reading message timeout in milliseconds.
    /// </summary>
    public int ReadMessageTimeoutInMilliseconds { get; set; } = 200;

    /// <summary>
    /// Number of messages saved within the message queue to receive.
    /// </summary>
    public ushort ReceiveMaximum { get; set; } = ushort.MaxValue;

    /// <summary>
    /// The time after a session expires when it's not actively used. UInt max value equals 136 years.
    /// </summary>
    public uint SessionExpiryInterval { get; set; } = uint.MaxValue;

    /// <summary>
    /// Username of the broker.
    /// </summary>
    public string Username { get; set; }
}