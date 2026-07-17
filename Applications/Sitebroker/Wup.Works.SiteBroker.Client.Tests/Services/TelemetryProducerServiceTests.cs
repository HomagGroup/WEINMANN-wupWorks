using Microsoft.Extensions.Options;
using NSubstitute;
using Wup.Works.SiteBroker.Client.Configuration;
using Wup.Works.SiteBroker.Client.Interfaces;
using Wup.Works.SiteBroker.Client.Models.Enums;
using Wup.Works.SiteBroker.Client.Services;

namespace Wup.Works.SiteBroker.Client.Tests.Services;

/// <summary>
/// Pins the wire contract: the exact topic each publish method targets and the exact JSON payload
/// the MES receives. A change here is a breaking change for every consumer.
/// </summary>
[TestClass]
public class TelemetryProducerServiceTests
{
    private readonly List<(string Topic, string Payload)> _published = [];
    private IMqttClientService _mqtt = null!;
    private TelemetryProducerService _producer = null!;

    [TestInitialize]
    public void Setup()
    {
        _published.Clear();
        _mqtt = Substitute.For<IMqttClientService>();
        _mqtt.Publish(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask)
            .AndDoes(call => _published.Add(((string)call[0], (string)call[1])));

        var options = Options.Create(new SiteBrokerOptions { MachineNumber = "M01" });
        _producer = new TelemetryProducerService(_mqtt, options);
    }

    [TestMethod]
    public async Task PublishMachineState_PublishesNumberPayloadOnStateTopic()
    {
        await _producer.PublishMachineState(MachineState.Working);

        var payload = PayloadFor("M01/telemetry/machine/state");
        StringAssert.Contains(payload, "\"type\":\"number\"");
        StringAssert.Contains(payload, "\"value\":\"3\"");     // Working = 3
        StringAssert.Contains(payload, "\"timestampUtc\":");
    }

    [TestMethod]
    public async Task PublishMachineParts_PublishesCounterAsNumber()
    {
        await _producer.PublishMachineParts(1234);

        var payload = PayloadFor("M01/telemetry/machine/parts");
        StringAssert.Contains(payload, "\"type\":\"number\"");
        StringAssert.Contains(payload, "\"value\":\"1234\"");
    }

    [TestMethod]
    public async Task PublishMachineProgram_PublishesStringPayload()
    {
        await _producer.PublishMachineProgram("Door_Left_800x2000");

        var payload = PayloadFor("M01/telemetry/machine/program");
        StringAssert.Contains(payload, "\"type\":\"string\"");
        StringAssert.Contains(payload, "\"value\":\"Door_Left_800x2000\"");
    }

    [TestMethod]
    public async Task PublishBatchVariantState_PublishesOnCurrentBatchVariantTopic()
    {
        await _producer.PublishBatchVariantState(BatchState.Active);

        var payload = PayloadFor("M01/telemetry/batch-variant/current/state");
        StringAssert.Contains(payload, "\"value\":\"2\"");     // Active = 2
    }

    [TestMethod]
    public async Task PublishToolType_PublishesToolTypeAsNumber()
    {
        await _producer.PublishToolType("10", ToolType.Fastening);

        var payload = PayloadFor("M01/telemetry/machine/tool/10/tool-type");
        StringAssert.Contains(payload, "\"value\":\"4\"");     // Fastening = 4
    }

    [TestMethod]
    public async Task PublishToolCounterType_PublishesCounterTypeAsNumber()
    {
        await _producer.PublishToolCounterType("10", CounterType.Hits);

        var payload = PayloadFor("M01/telemetry/machine/tool/10/counter-type");
        StringAssert.Contains(payload, "\"value\":\"3\"");     // Hits = 3
    }

    [TestMethod]
    public async Task PublishToolTime_PublishesOperatingTimeInSeconds()
    {
        await _producer.PublishToolTime("10", 128356);

        var payload = PayloadFor("M01/telemetry/machine/tool/10/time");
        StringAssert.Contains(payload, "\"type\":\"number\"");
        StringAssert.Contains(payload, "\"value\":\"128356\"");
    }

    [TestMethod]
    public async Task PublishErrorDescription_PublishesOnIndexedErrorTopic()
    {
        await _producer.PublishErrorDescription("E01", "Air pressure too low");

        var payload = PayloadFor("M01/telemetry/machine/error/E01/description");
        StringAssert.Contains(payload, "\"type\":\"string\"");
        StringAssert.Contains(payload, "\"value\":\"Air pressure too low\"");
    }

    [TestMethod]
    public async Task RemoveErrorDescription_PublishesEmptyPayload()
    {
        await _producer.RemoveErrorDescription("E01");

        // Removal convention: an empty retained payload clears the entry.
        Assert.AreEqual(string.Empty, PayloadFor("M01/telemetry/machine/error/E01/description"));
    }

    [TestMethod]
    public async Task RemoveTool_ClearsAllToolTopics()
    {
        await _producer.RemoveTool("10");

        string[] expected =
        [
            "M01/telemetry/machine/tool/10/description",
            "M01/telemetry/machine/tool/10/tool-type",
            "M01/telemetry/machine/tool/10/counter-type",
            "M01/telemetry/machine/tool/10/counter",
            "M01/telemetry/machine/tool/10/time"
        ];

        foreach (var topic in expected)
        {
            Assert.AreEqual(string.Empty, PayloadFor(topic), $"expected empty payload on {topic}");
        }
    }

    [TestMethod]
    public async Task RemoveStorage_ClearsAllThreeStorageTopics()
    {
        await _producer.RemoveStorage("S1");

        string[] expected =
        [
            "M01/telemetry/machine/storage/S1/level",
            "M01/telemetry/machine/storage/S1/capacity",
            "M01/telemetry/machine/storage/S1/material"
        ];

        foreach (var topic in expected)
        {
            Assert.AreEqual(string.Empty, PayloadFor(topic), $"expected empty payload on {topic}");
        }
    }

    private string PayloadFor(string topic)
    {
        var matches = _published.Where(published => published.Topic == topic).ToList();
        Assert.AreEqual(1, matches.Count, $"expected exactly one publish on {topic}");
        return matches[0].Payload;
    }
}
