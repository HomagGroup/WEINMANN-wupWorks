using System.Globalization;
using Wup.Works.SiteBroker.Client.Models;
using Wup.Works.SiteBroker.Client.Models.Enums;
using Wup.Works.SiteBroker.Client.Models.Payload;

namespace Wup.Works.SiteBroker.Client.Tests.Models;

/// <summary>
/// Values travel the wire as strings; GetValueAs converts them back for the consumer.
/// </summary>
[TestClass]
public class TelemetryTopicTests
{
    [TestMethod]
    [DataRow("42", 42)]
    [DataRow("0", 0)]
    public void GetValueAs_ConvertsToInt(string wireValue, int expected)
        => Assert.AreEqual(expected, Topic(wireValue, TelemetryValueType.Number).GetValueAs<int>());

    [TestMethod]
    [DataRow("633.77", 633.77)]
    public void GetValueAs_ConvertsToDouble(string wireValue, double expected)
        => Assert.AreEqual(expected, Topic(wireValue, TelemetryValueType.Number).GetValueAs<double>());

    [TestMethod]
    [DataRow("Door_Left_800x2000")]
    public void GetValueAs_ReturnsStringUnchanged(string wireValue)
        => Assert.AreEqual(wireValue, Topic(wireValue, TelemetryValueType.String).GetValueAs<string>());

    [TestMethod]
    [DataRow("2026-07-09T10:15:30.000Z")]
    public void GetValueAs_ConvertsToDateTime(string wireValue)
    {
        var value = Topic(wireValue, TelemetryValueType.String).GetValueAs<DateTime>();
        Assert.AreEqual(2026, value.Year);
        Assert.AreEqual(7, value.Month);
        Assert.AreEqual(9, value.Day);
    }

    [TestMethod]
    [DataRow("633.77", 633.77)]
    public void GetValueAs_IsCultureInvariant(string wireValue, double expected)
    {
        // The wire format always uses a decimal point, even on comma-decimal cultures.
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.AreEqual(expected, Topic(wireValue, TelemetryValueType.Number).GetValueAs<double>());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    private static TelemetryTopic Topic(string value, TelemetryValueType type) => new()
    {
        Path = "M01/telemetry/machine/state",
        Payload = new TelemetryPayloadDto { Type = type, Value = value }
    };
}
