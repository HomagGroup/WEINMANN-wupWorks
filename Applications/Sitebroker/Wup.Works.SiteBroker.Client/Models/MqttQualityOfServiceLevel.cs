namespace Wup.Works.SiteBroker.Client.Models;

/// <summary>
/// The Quality of Service (QoS) level is an agreement between the sender of a message and the receiver of a message
/// that defines the guarantee of delivery for a specific message.
/// There are 3 QoS levels in MQTT:
/// - At most once (0): Message gets delivered no time, once or multiple times.
/// - At least once (1): Message gets delivered at least once (One time or more often).
/// - Exactly once  (2): Message gets delivered exactly once (It's ensured that the message only comes once).
/// </summary>
public enum MqttQualityOfServiceLevel
{
    AtMostOnce = 0x00,
    AtLeastOnce = 0x01,
    ExactlyOnce = 0x02
}