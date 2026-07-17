using Wup.Works.SiteBroker.Client.Helpers;

namespace Wup.Works.SiteBroker.Client.Tests.Helpers;

/// <summary>
/// Command-channel topics are directional: requests are {orchestrator}/{machine}/..., responses
/// are {machine}/{orchestrator}/... Pin that, since swapping them silently breaks routing.
/// </summary>
[TestClass]
public class OrderTopicHelperTests
{
    private const string Machine = "M01";
    private const string Orchestrator = "orchestrator";

    [TestMethod]
    public void GetOrderLoadTopic_IsAddressedToTheMachine()
        => Assert.AreEqual(
            "orchestrator/M01/order/123/load",
            OrderTopicHelper.GetOrderLoadTopic(Machine, Orchestrator, "123"));

    [TestMethod]
    public void GetOrderLoadedTopic_IsAddressedToTheOrchestrator()
        => Assert.AreEqual(
            "M01/orchestrator/order/123/loaded",
            OrderTopicHelper.GetOrderLoadedTopic(Machine, Orchestrator, "123"));

    [TestMethod]
    public void GetBatchPrepareTopic_IsAddressedToTheMachine()
        => Assert.AreEqual(
            "orchestrator/M01/batch/B7/prepare",
            OrderTopicHelper.GetBatchPrepareTopic(Machine, Orchestrator, "B7"));

    [TestMethod]
    public void GetBatchPreparedTopic_IsAddressedToTheOrchestrator()
        => Assert.AreEqual(
            "M01/orchestrator/batch/B7/prepared",
            OrderTopicHelper.GetBatchPreparedTopic(Machine, Orchestrator, "B7"));

    [TestMethod]
    public void GetBatchVariantProduceTopic_IsAddressedToTheMachine()
        => Assert.AreEqual(
            "orchestrator/M01/batch-variant/V9/produce",
            OrderTopicHelper.GetBatchVariantProduceTopic(Machine, Orchestrator, "V9"));

    [TestMethod]
    public void GetBatchVariantProducedTopic_IsAddressedToTheOrchestrator()
        => Assert.AreEqual(
            "M01/orchestrator/batch-variant/V9/produced",
            OrderTopicHelper.GetBatchVariantProducedTopic(Machine, Orchestrator, "V9"));

    [TestMethod]
    public void GetOnlineModeTopic_BuildsSettingsTopic()
        => Assert.AreEqual(
            "M01/orchestrator/settings/online-mode",
            OrderTopicHelper.GetOnlineModeTopic(Machine, Orchestrator));

    [TestMethod]
    [DataRow("orchestrator/+/order/+/load", "orchestrator/M01/order/123/load")]
    [DataRow("orchestrator/M01/order/+/load", "orchestrator/M01/order/123/load")]
    [DataRow("orchestrator/M01/order/123/load", "orchestrator/M01/order/123/load")]
    public void ValidateTopic_MatchesWildcards(string subscribed, string received)
        => Assert.IsTrue(subscribed.ValidateTopic(received));

    [TestMethod]
    [DataRow("orchestrator/+/order/+/load", "orchestrator/M01/order/123/loaded")]  // different action
    [DataRow("orchestrator/+/batch/+/prepare", "orchestrator/M01/order/123/load")] // different resource
    [DataRow("orchestrator/+/order/+/load", "orchestrator/M01/order/load")]        // segment count differs
    public void ValidateTopic_RejectsNonMatchingTopics(string subscribed, string received)
        => Assert.IsFalse(subscribed.ValidateTopic(received));
}
