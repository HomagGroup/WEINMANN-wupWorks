namespace Wup.Works.SiteBroker.Client;

public static class Constants
{
    public const double HttpTimeoutSeconds = 5;

    public const string Wildcard = "+";

    public const string Orchestrator = "orchestrator";

    public const string OrderId = "OrderId";

    public const string BatchId = "BatchId";

    public const string BatchVariantId = "BatchVariantId";

    public const string Filename = "Filename";

    public const string Variant = "Variant";

    #region Data channel

    /// <summary>
    /// Fixed namespace segment of every machine data topic.
    /// </summary>
    public const string DataNamespace = "data";

    /// <summary>
    /// Multi-level MQTT wildcard, used to subscribe to whole data subtrees.
    /// </summary>
    public const string MultiLevelWildcard = "#";

    // Payload value types.
    public const string TypeNumber = "number";
    public const string TypeString = "string";

    #endregion Data channel
}
