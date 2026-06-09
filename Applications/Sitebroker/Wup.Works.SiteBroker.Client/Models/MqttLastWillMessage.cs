namespace Wup.Works.SiteBroker.Client.Models;

/// <summary>
/// Defines the last will message of the client.
/// </summary>
public sealed class MqttLastWillMessage
{
    #region Properties

    /// <summary>
    /// Payload
    /// </summary>
    public string Payload { get; set; }

    /// <summary>
    /// Quality of service level.
    /// </summary>
    public MqttQualityOfServiceLevel Qos { get; set; }

    /// <summary>
    /// Retain
    /// </summary>
    public bool Retain { get; set; }

    /// <summary>
    /// Topic
    /// </summary>
    public string Topic { get; set; }

    #endregion
}