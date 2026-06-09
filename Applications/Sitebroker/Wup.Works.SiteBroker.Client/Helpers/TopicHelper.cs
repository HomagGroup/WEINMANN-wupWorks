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
        => $"{machineNumber}/{orchestrator}/batch/{batchId}/perpared";

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