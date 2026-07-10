using Microsoft.Extensions.Options;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;
using Wup.Works.SiteBroker.Client.Configuration;
using Wup.Works.SiteBroker.Client.Helpers;
using Wup.Works.SiteBroker.Client.Interfaces;
using Wup.Works.SiteBroker.Client.Models;
using Wup.Works.SiteBroker.Client.Models.Payload;

namespace Wup.Works.SiteBroker.Client.Services;

/// <inheritdoc cref="ITelemetryConsumerService" />
public class TelemetryConsumerService : ITelemetryConsumerService, IDisposable
{
    private readonly IMqttClientService _mqttClientService;
    private readonly string _subscriptionTopic;

    public TelemetryConsumerService(IMqttClientService mqttClientService, IOptions<SiteBrokerOptions> options)
    {
        _mqttClientService = mqttClientService;
        _subscriptionTopic = TelemetryTopicHelper.GetTelemetrySubscriptionTopic(
            options.Value.SubscribeToAllMachinesData ? Constants.Wildcard : options.Value.MachineNumber);
    }

    public event EventHandler<TelemetryReceivedEventArgs>? TelemetryReceived;

    public async Task Connect()
    {
        await _mqttClientService.Connect();
        _mqttClientService.MessageReceived += MessageReceived;

        await _mqttClientService.Subscribe(_subscriptionTopic);
    }

    public async Task Disconnect()
    {
        await _mqttClientService.Unsubscribe(_subscriptionTopic);
        _mqttClientService.MessageReceived -= MessageReceived;
    }

    public async void Dispose()
        => await Disconnect();

    private void MessageReceived(object? sender, MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;

        // Ignore everything that does not belong to the telemetry channel (e.g. command topics).
        if (!TelemetryTopicHelper.IsTelemetryTopic(topic))
            return;

        var telemetry = new TelemetryTopic { Path = topic };
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

        if (string.IsNullOrEmpty(payload))
        {
            TelemetryReceived?.Invoke(this, new TelemetryReceivedEventArgs { Topic = telemetry, Removed = true });
            return;
        }

        TelemetryPayloadDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<TelemetryPayloadDto>(payload, TelemetrySerialization.Options);
        }
        catch (JsonException)
        {
            // Malformed payload on the (broadcast) telemetry channel — skip it rather than
            // fault the shared message handler and break processing of other topics.
            return;
        }

        if (dto != null)
        {
            telemetry.Payload = dto;
        }

        TelemetryReceived?.Invoke(this, new TelemetryReceivedEventArgs { Topic = telemetry });
    }
}
