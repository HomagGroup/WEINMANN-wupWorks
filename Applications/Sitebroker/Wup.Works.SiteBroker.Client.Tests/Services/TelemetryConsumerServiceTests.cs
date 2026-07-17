using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using NSubstitute;
using Wup.Works.SiteBroker.Client.Configuration;
using Wup.Works.SiteBroker.Client.Interfaces;
using Wup.Works.SiteBroker.Client.Models;
using Wup.Works.SiteBroker.Client.Models.Enums;
using Wup.Works.SiteBroker.Client.Services;

namespace Wup.Works.SiteBroker.Client.Tests.Services;

/// <summary>
/// The consumer is the MES side: it must parse telemetry payloads, ignore command-channel topics,
/// surface removals, and never fault the shared handler on a malformed payload.
/// </summary>
[TestClass]
public class TelemetryConsumerServiceTests
{
    private IMqttClientService _mqtt = null!;
    private TelemetryConsumerService _consumer = null!;
    private readonly List<TelemetryReceivedEventArgs> _received = [];

    [TestInitialize]
    public async Task Setup()
    {
        _received.Clear();
        _mqtt = Substitute.For<IMqttClientService>();

        var options = Options.Create(new SiteBrokerOptions
        {
            MachineNumber = "M01",
            SubscribeToAllMachinesData = true
        });

        _consumer = new TelemetryConsumerService(_mqtt, options);
        _consumer.TelemetryReceived += (_, args) => _received.Add(args);

        await _consumer.Connect();
    }

    [TestMethod]
    public async Task Connect_SubscribesToAllMachinesTelemetrySubtree()
        => await _mqtt.Received(1).Subscribe("+/telemetry/#");

    [TestMethod]
    public void MessageReceived_TelemetryTopic_RaisesParsedTelemetry()
    {
        Receive(
            "M01/telemetry/machine/state",
            """{"type":"number","value":"3","timestampUtc":"2026-07-09T10:00:00Z"}""");

        Assert.AreEqual(1, _received.Count);
        var topic = _received[0].Topic;
        Assert.AreEqual("M01/telemetry/machine/state", topic.Path);
        Assert.AreEqual(TelemetryValueType.Number, topic.Payload.Type);
        Assert.AreEqual("3", topic.Payload.Value);
        Assert.AreEqual(3, topic.GetValueAs<int>());
        Assert.IsFalse(_received[0].Removed);
    }

    [TestMethod]
    public void MessageReceived_StringPayload_ParsesStringType()
    {
        Receive(
            "M01/telemetry/machine/program",
            """{"type":"string","value":"Door_Left_800x2000","timestampUtc":"2026-07-09T10:00:00Z"}""");

        Assert.AreEqual(1, _received.Count);
        Assert.AreEqual(TelemetryValueType.String, _received[0].Topic.Payload.Type);
        Assert.AreEqual("Door_Left_800x2000", _received[0].Topic.Payload.Value);
    }

    [TestMethod]
    public void MessageReceived_EmptyPayload_SignalsRemoval()
    {
        Receive("M01/telemetry/machine/error/E01/description", string.Empty);

        Assert.AreEqual(1, _received.Count);
        Assert.IsTrue(_received[0].Removed);
        Assert.AreEqual("M01/telemetry/machine/error/E01/description", _received[0].Topic.Path);
    }

    [TestMethod]
    public void MessageReceived_CommandChannelTopic_IsIgnored()
    {
        Receive("orchestrator/M01/order/123/load", """{"status":1}""");

        Assert.AreEqual(0, _received.Count);
    }

    [TestMethod]
    public void MessageReceived_MalformedPayload_IsSwallowed()
    {
        // Must not throw: the handler is shared, so faulting would break other topics.
        Receive("M01/telemetry/machine/state", "{not-json");

        Assert.AreEqual(0, _received.Count);
    }

    private void Receive(string topic, string payload)
        => _mqtt.MessageReceived += Raise.EventWith(new object(), MessageArgs(topic, payload));

    private static MqttApplicationMessageReceivedEventArgs MessageArgs(string topic, string payload)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build();

        return new MqttApplicationMessageReceivedEventArgs(
            "test-client",
            message,
            new MqttPublishPacket(),
            (_, _) => Task.CompletedTask);
    }
}
