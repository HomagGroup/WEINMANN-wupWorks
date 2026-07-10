namespace Wup.Works.SiteBroker.Client;

public static class Constants
{
    #region General

    public const double HttpTimeoutSeconds = 5;

    public const string Wildcard = "+";

    public const string Orchestrator = "orchestrator";

    #endregion General

    #region Payload keys

    public const string OrderId = "OrderId";

    public const string BatchId = "BatchId";

    public const string BatchVariantId = "BatchVariantId";

    public const string Filename = "Filename";

    public const string Variant = "Variant";

    #endregion Payload keys

    #region Telemetry

    /// <summary>
    /// Fixed namespace segment of every telemetry topic.
    /// </summary>
    public const string TelemetryNamespace = "telemetry";

    /// <summary>
    /// Multi-level MQTT wildcard, used to subscribe to whole telemetry subtrees.
    /// </summary>
    public const string MultiLevelWildcard = "#";

    #endregion Telemetry
}
