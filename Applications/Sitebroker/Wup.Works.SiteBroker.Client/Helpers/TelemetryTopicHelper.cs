using Wup.Works.SiteBroker.Client.Models;

namespace Wup.Works.SiteBroker.Client.Helpers;

/// <summary>
/// Builds and matches the telemetry topics. Topic forms:
///   {sender}/telemetry/machine/{key}                        (machine single value)
///   {sender}/telemetry/machine/{group}/{instance}/{key}     (machine indexed entry)
///   {sender}/telemetry/batch-variant/current/{key}          (current batch variant)
/// </summary>
public static class TelemetryTopicHelper
{
    private const string CategoryMachine = "machine";
    private const string CategoryBatchVariant = "batch-variant";
    private const string GroupError = "error";
    private const string GroupWarning = "warning";
    private const string GroupMaintenance = "maintenance";
    private const string GroupAction = "action";
    private const string GroupStorage = "storage";
    private const string GroupTool = "tool";
    private const string BatchVariantInstance = "current";

    /// <summary>
    /// Topic of a single-value machine property (<c>{sender}/telemetry/machine/{key}</c>),
    /// e.g. <c>state</c>, <c>parts</c>, <c>program</c>.
    /// </summary>
    public static TelemetryTopic CreateMachineTelemetryTopic(string sender, string key)
        => new() { Path = $"{sender}/{Constants.TelemetryNamespace}/{CategoryMachine}/{key}" };

    /// <summary>Topic of one error entry property (<c>.../machine/error/{instance}/{key}</c>).</summary>
    public static TelemetryTopic CreateErrorTelemetryTopic(string sender, string instance, string key)
        => CreateGroupTopic(sender, GroupError, instance, key);

    /// <summary>Topic of one warning entry property (<c>.../machine/warning/{instance}/{key}</c>).</summary>
    public static TelemetryTopic CreateWarningTelemetryTopic(string sender, string instance, string key)
        => CreateGroupTopic(sender, GroupWarning, instance, key);

    /// <summary>Topic of one maintenance entry property (<c>.../machine/maintenance/{instance}/{key}</c>).</summary>
    public static TelemetryTopic CreateMaintenanceTelemetryTopic(string sender, string instance, string key)
        => CreateGroupTopic(sender, GroupMaintenance, instance, key);

    /// <summary>Topic of one action entry property (<c>.../machine/action/{instance}/{key}</c>).</summary>
    public static TelemetryTopic CreateActionTelemetryTopic(string sender, string instance, string key)
        => CreateGroupTopic(sender, GroupAction, instance, key);

    /// <summary>Topic of one storage entry property (<c>.../machine/storage/{instance}/{key}</c>).</summary>
    public static TelemetryTopic CreateStorageTelemetryTopic(string sender, string instance, string key)
        => CreateGroupTopic(sender, GroupStorage, instance, key);

    /// <summary>Topic of one tool entry property (<c>.../machine/tool/{instance}/{key}</c>).</summary>
    public static TelemetryTopic CreateToolTelemetryTopic(string sender, string instance, string key)
        => CreateGroupTopic(sender, GroupTool, instance, key);

    /// <summary>
    /// Topic of one property of the current batch variant
    /// (<c>{sender}/telemetry/batch-variant/current/{key}</c>), e.g. <c>id</c>, <c>state</c>, <c>progress</c>.
    /// </summary>
    public static TelemetryTopic CreateBatchVariantTelemetryTopic(string sender, string key)
        => new() { Path = $"{sender}/{Constants.TelemetryNamespace}/{CategoryBatchVariant}/{BatchVariantInstance}/{key}" };

    /// <summary>
    /// Subscription topic covering the whole telemetry subtree of a machine.
    /// Pass <see cref="Constants.Wildcard"/> as the sender to cover all machines.
    /// </summary>
    public static string GetTelemetrySubscriptionTopic(string sender)
        => $"{sender}/{Constants.TelemetryNamespace}/{Constants.MultiLevelWildcard}";

    /// <summary>
    /// Whether a received topic belongs to the telemetry channel (correct namespace and
    /// one of the valid segment counts).
    /// </summary>
    public static bool IsTelemetryTopic(string topic)
    {
        if (string.IsNullOrEmpty(topic))
            return false;

        var parts = topic.Split('/');

        return parts.Length is 4 or 5 or 6
            && parts[1].Equals(Constants.TelemetryNamespace, StringComparison.InvariantCultureIgnoreCase);
    }

    private static TelemetryTopic CreateGroupTopic(string sender, string group, string instance, string key)
        => new() { Path = $"{sender}/{Constants.TelemetryNamespace}/{CategoryMachine}/{group}/{instance}/{key}" };
}
