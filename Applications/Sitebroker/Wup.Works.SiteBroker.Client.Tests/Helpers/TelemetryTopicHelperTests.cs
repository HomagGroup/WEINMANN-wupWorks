using Wup.Works.SiteBroker.Client.Helpers;

namespace Wup.Works.SiteBroker.Client.Tests.Helpers;

/// <summary>
/// The telemetry topic paths are the contract with the MES: pin them.
/// </summary>
[TestClass]
public class TelemetryTopicHelperTests
{
    [TestMethod]
    [DataRow("M01", "state", "M01/telemetry/machine/state")]
    [DataRow("M01", "parts", "M01/telemetry/machine/parts")]
    [DataRow("DEMO-01", "program", "DEMO-01/telemetry/machine/program")]
    public void CreateMachineTelemetryTopic_BuildsMachinePath(string sender, string key, string expected)
        => Assert.AreEqual(expected, TelemetryTopicHelper.CreateMachineTelemetryTopic(sender, key).Path);

    [TestMethod]
    public void CreateErrorTelemetryTopic_BuildsIndexedGroupPath()
        => Assert.AreEqual(
            "M01/telemetry/machine/error/E01/description",
            TelemetryTopicHelper.CreateErrorTelemetryTopic("M01", "E01", "description").Path);

    [TestMethod]
    public void CreateWarningTelemetryTopic_BuildsIndexedGroupPath()
        => Assert.AreEqual(
            "M01/telemetry/machine/warning/W01/description",
            TelemetryTopicHelper.CreateWarningTelemetryTopic("M01", "W01", "description").Path);

    [TestMethod]
    public void CreateMaintenanceTelemetryTopic_BuildsIndexedGroupPath()
        => Assert.AreEqual(
            "M01/telemetry/machine/maintenance/M99/description",
            TelemetryTopicHelper.CreateMaintenanceTelemetryTopic("M01", "M99", "description").Path);

    [TestMethod]
    public void CreateActionTelemetryTopic_BuildsIndexedGroupPath()
        => Assert.AreEqual(
            "M01/telemetry/machine/action/A01/description",
            TelemetryTopicHelper.CreateActionTelemetryTopic("M01", "A01", "description").Path);

    [TestMethod]
    public void CreateStorageTelemetryTopic_BuildsIndexedGroupPath()
        => Assert.AreEqual(
            "M01/telemetry/machine/storage/S1/level",
            TelemetryTopicHelper.CreateStorageTelemetryTopic("M01", "S1", "level").Path);

    [TestMethod]
    public void CreateToolTelemetryTopic_BuildsIndexedGroupPath()
        => Assert.AreEqual(
            "M01/telemetry/machine/tool/10/counter",
            TelemetryTopicHelper.CreateToolTelemetryTopic("M01", "10", "counter").Path);

    [TestMethod]
    public void CreateBatchVariantTelemetryTopic_UsesCurrentInstance()
        => Assert.AreEqual(
            "M01/telemetry/batch-variant/current/progress",
            TelemetryTopicHelper.CreateBatchVariantTelemetryTopic("M01", "progress").Path);

    [TestMethod]
    [DataRow("M01", "M01/telemetry/#")]
    [DataRow("+", "+/telemetry/#")]
    public void GetTelemetrySubscriptionTopic_CoversWholeSubtree(string sender, string expected)
        => Assert.AreEqual(expected, TelemetryTopicHelper.GetTelemetrySubscriptionTopic(sender));

    [TestMethod]
    [DataRow("M01/telemetry/machine/state")]                    // 4 segments
    [DataRow("M01/telemetry/batch-variant/current/progress")]   // 5 segments
    [DataRow("M01/telemetry/machine/tool/10/counter")]          // 6 segments
    public void IsTelemetryTopic_AcceptsTelemetryTopics(string topic)
        => Assert.IsTrue(TelemetryTopicHelper.IsTelemetryTopic(topic));

    [TestMethod]
    [DataRow("orchestrator/M01/order/123/load")]  // command channel, not telemetry
    [DataRow("M01/orchestrator/settings/online-mode")]
    [DataRow("M01/telemetry")]                    // too few segments
    [DataRow("")]
    public void IsTelemetryTopic_RejectsNonTelemetryTopics(string topic)
        => Assert.IsFalse(TelemetryTopicHelper.IsTelemetryTopic(topic));
}
