using MQTTnet.Client;
using Wup.Works.SiteBroker.Client.Models;

namespace Wup.Works.SiteBroker.Client.Interfaces;

/// <summary>
/// Providing basic MQTT client functionality.
/// </summary>
public interface IMqttClientService : IDisposable
{
    /// <summary>
    /// Gets invoked when the MQTT client was connected successfully.
    /// </summary>
    public event EventHandler? Connected;

    /// <summary>
    /// Gets invoked when the MQTT client was disconnected.
    /// </summary>
    public event EventHandler? Disconnected;

    /// <summary>
    /// General message receive handling. Gets invoked when the MQTT client receives a message.
    /// </summary>
    public event EventHandler<MqttApplicationMessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// If not already connected, connects to the MQTT broker using the options, defined in the app settings (section
    /// "Mqtt").
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task Connect();

    /// <summary>
    /// Executes a clean disconnect of the MQTT client.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task Disconnect();

    /// <summary>
    /// When connected, publishes a message to the MQTT broker on the specified topic.
    /// </summary>
    /// <param name="topic">Topic for addressing.</param>
    /// <param name="payload">The payload contains the actual content of the message.</param>
    /// <param name="retainFlag">If set to true, the broker retains the message for later subscriptions.</param>
    /// <param name="qos">Quality of service level (0, 1 or 2).</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task Publish(string topic, string payload, bool retainFlag, int qos);

    /// <summary>
    /// When connected, publishes a message to the MQTT broker on the specified topic with quality of service level 2.
    /// </summary>
    /// <param name="topic">Topic for addressing.</param>
    /// <param name="payload">The payload contains the actual content of the message.</param>
    /// <param name="retainFlag">If set to true, the broker retains the message for later subscriptions.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task Publish(string topic, string payload, bool retainFlag);

    /// <summary>
    /// When connected, publishes a message to the MQTT broker on the specified topic with retain flag set to true.
    /// </summary>
    /// <param name="topic">Topic for addressing.</param>
    /// <param name="payload">The payload contains the actual content of the message.</param>
    /// <param name="qos">Quality of service level (0, 1 or 2).</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task Publish(string topic, string payload, int qos);

    /// <summary>
    /// When connected, publishes a message to the MQTT broker on the specified topic with retain flag set to true and
    /// quality of service level 2.
    /// </summary>
    /// <param name="topic">Topic for addressing.</param>
    /// <param name="payload">The payload contains the actual content of the message.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task Publish(string topic, string payload);

    /// <summary>
    /// Sets the last will message. If the client was already connected, it gets disconnected and reconnected to set the
    /// new last will message. The task waits until the Connected event gets fired.
    /// If you want to preserve the session after reconnect, CleanSession in the MqttOptions should be set to false.
    /// </summary>
    /// <param name="lastWillMessage">Last will message to be set.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="MqttCommunicationException">Throws if client is not connected.</exception>
    public Task SetLastWillMessage(MqttLastWillMessage lastWillMessage);

    /// <summary>
    /// When connected, subscribes to the specified topic on the MQTT broker.
    /// </summary>
    /// <param name="topic">Topic to subscribe to.</param>
    /// <param name="qos">Quality of service level (<c>0</c>, <c>1</c> or <c>2</c>).</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="MqttCommunicationException">Throws if client is not connected.</exception>
    public Task Subscribe(string topic, int qos);

    /// <summary>
    /// When connected, subscribes to the specified topic on the MQTT broker with quality of service level 2.
    /// </summary>
    /// <param name="topic">Topic to subscribe to.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="MqttCommunicationException">Throws if client is not connected.</exception>
    public Task Subscribe(string topic);

    /// <summary>
    /// When connected, unsubscribes from the stated topic on the MQTT broker if it is not used anywhere else.
    /// </summary>
    /// <param name="topic">Topic to unsubscribe from.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="MqttCommunicationException">Throws if client is not connected.</exception>
    public Task Unsubscribe(string topic);
}