using Wup.Works.SiteBroker.Client.Models;
using Wup.Works.SiteBroker.Client.Models.Enums;

namespace Wup.Works.SiteBroker.Client.Helpers;

public static class TopicHelper
{

    /// <summary>
    /// Get the topic name when an order is being requested to be loaded
    /// </summary>
    /// <param name="machineNumber">Machine number of the wupWorks machine</param>
    /// <param name="orchestrator">Name of the orchestrator within the system</param>
    /// <param name="orderId">Order id to be loaded</param>
    /// <returns>The topic name</returns>
    public static string GetOrderLoadTopic(string machineNumber, string orchestrator, string orderId)
        => $"{orchestrator}/{machineNumber}/order/{orderId}/load";

    /// <summary>
    /// Get the topic name when an order was loadeded
    /// </summary>
    /// <param name="machineNumber">Machine number of the wupWorks machine</param>
    /// <param name="orchestrator">Name of the orchestrator within the system</param>
    /// <param name="orderId">Order id loaded</param>
    /// <returns>The topic name</returns>
    public static string GetOrderLoadedTopic(string machineNumber, string orchestrator, string orderId)
        => $"{machineNumber}/{orchestrator}/order/{orderId}/loaded";

    /// <summary>
    /// Get the topic name when a batch is being requested to be prepared
    /// </summary>
    /// <param name="machineNumber">Machine number of the wupWorks machine</param>
    /// <param name="orchestrator">Name of the orchestrator within the system</param>
    /// <param name="batchId">Batch id to be prepared</param>
    /// <returns>The topic name</returns>
    public static string GetBatchPrepareTopic(string machineNumber, string orchestrator, string batchId)
        => $"{orchestrator}/{machineNumber}/batch/{batchId}/prepare";

    /// <summary>
    /// Get the topic name when a batch is being requested was prepared
    /// </summary>
    /// <param name="machineNumber">Machine number of the wupWorks machine</param>
    /// <param name="orchestrator">Name of the orchestrator within the system</param>
    /// <param name="batchId">Batch id prepared</param>
    /// <returns>The topic name</returns>
    public static string GetBatchPreparedTopic(string machineNumber, string orchestrator, string batchId)
        => $"{machineNumber}/{orchestrator}/batch/{batchId}/prepared";

    /// <summary>
    /// Get the topic name when a batch variant is being requested to be produced
    /// </summary>
    /// <param name="machineNumber">Machine number of the wupWorks machine</param>
    /// <param name="orchestrator">Name of the orchestrator within the system</param>
    /// <param name="batchVariantId">Batch variant id to be produced</param>
    /// <returns>The topic name</returns>
    public static string GetBatchVariantProduceTopic(string machineNumber, string orchestrator, string batchVariantId)
        => $"{orchestrator}/{machineNumber}/batch-variant/{batchVariantId}/produce";

    /// <summary>
    /// Get the topic name when a batch variant was produced
    /// </summary>
    /// <param name="machineNumber">Machine number of the wupWorks machine</param>
    /// <param name="orchestrator">Name of the orchestrator within the system</param>
    /// <param name="batchVariantId">Batch variant id produced</param>
    /// <returns>The topic name</returns>
    public static string GetBatchVariantProducedTopic(string machineNumber, string orchestrator, string batchVariantId)
        => $"{machineNumber}/{orchestrator}/batch-variant/{batchVariantId}/produced";

    /// <summary>
    /// Get the topic name for the status of the online mode
    /// </summary>
    /// <param name="machineNumber">Machine number of the wupWorks machine</param>
    /// <param name="orchestrator">Name of the orchestrator within the system</param>
    /// <returns>The topic name</returns>
    public static string GetOnlineModeTopic(string machineNumber, string orchestrator)
        => $"{machineNumber}/{orchestrator}/settings/online-mode";

    #region Data channel

    /// <summary>
    /// Get the topic of a single-value data signal
    /// (<c>{MachineNumber}/data/{Category}/{Signal}</c>).
    /// </summary>
    public static string GetDataTopic(string machineNumber, DataCategory category, DataSignal signal)
        => $"{machineNumber}/{Constants.DataNamespace}/{category.ToTopicSegment()}/{signal.ToTopicSegment()}";

    /// <summary>
    /// Get the topic of one entry of an indexed data signal group
    /// (<c>{MachineNumber}/data/{Category}/{Group}/{Key}</c>).
    /// </summary>
    public static string GetDataIndexedTopic(string machineNumber, DataCategory category, DataGroup group, string key)
        => $"{machineNumber}/{Constants.DataNamespace}/{category.ToTopicSegment()}/{group.ToTopicSegment()}/{key}";

    /// <summary>
    /// Get the topic of one property of an indexed data entry
    /// (<c>{MachineNumber}/data/{Category}/{Group}/{Key}/{Property}</c>).
    /// </summary>
    public static string GetDataPropertyTopic(string machineNumber, DataCategory category, DataGroup group, string key, StorageProperty property)
        => $"{machineNumber}/{Constants.DataNamespace}/{category.ToTopicSegment()}/{group.ToTopicSegment()}/{key}/{property.ToTopicSegment()}";

    /// <summary>
    /// Get the subscription topic covering the whole data subtree of a machine.
    /// Pass <see cref="Constants.Wildcard"/> as the machine number to cover all machines.
    /// </summary>
    public static string GetDataSubscriptionTopic(string machineNumber)
        => $"{machineNumber}/{Constants.DataNamespace}/{Constants.MultiLevelWildcard}";

    /// <summary>
    /// Try to decompose a received data topic into its parts. Returns <c>false</c> for
    /// topics that do not belong to the data channel.
    /// </summary>
    /// <param name="topic">The received (wildcard-free) topic.</param>
    /// <param name="parsed">The decomposed topic when parsing succeeds.</param>
    public static bool TryParseDataTopic(string topic, out ParsedDataTopic parsed)
    {
        parsed = null;

        if (string.IsNullOrEmpty(topic))
            return false;

        var parts = topic.Split('/');

        // Valid forms: 4 (single value), 5 (indexed entry) or 6 (indexed property) segments.
        if (parts.Length is < 4 or > 6)
            return false;

        if (!parts[1].Equals(Constants.DataNamespace, StringComparison.InvariantCultureIgnoreCase))
            return false;

        parsed = new ParsedDataTopic
        {
            MachineNumber = parts[0],
            Category = parts[2],
            Name = parts[3],
            Key = parts.Length >= 5 ? parts[4] : null,
            Property = parts.Length == 6 ? parts[5] : null
        };

        return true;
    }

    #endregion Data channel

    /// <summary>
    /// Validate if a topic name matched with the subscribed topic name containing wildcards.
    /// </summary>
    /// <param name="subscribedTopic">Subscribed topic containing wildcards</param>
    /// <param name="receivedTopic">Received topic not containing wildcards</param>
    /// <returns>Bool if topic matches</returns>
    public static bool ValidateTopic(this string subscribedTopic, string receivedTopic)
    {
        var subscribedTopicParts = subscribedTopic.Split("/");
        var receivedTopicParts = receivedTopic.Split("/");

        if (subscribedTopicParts.Count() != receivedTopicParts.Count())
            return false;

        for (int i = 0; i < subscribedTopicParts.Count(); i++)
        {
            if (subscribedTopicParts[i].Equals(Constants.Wildcard) 
                || subscribedTopicParts[i].Equals(receivedTopicParts[i], StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }

            return false;
        }

        return true;
    }
}