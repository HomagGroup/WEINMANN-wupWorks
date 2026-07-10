using Wup.Works.SiteBroker.Client.Models;
using Wup.Works.SiteBroker.Client.Models.Enums;

namespace Wup.Works.SiteBroker.Client.Interfaces;

/// <summary>
/// Publishes machine telemetry (states, counters, alerts, storage, batch progress) to the telemetry
/// channel. All messages are published retained and the producer stamps the timestamp itself.
/// </summary>
public interface ITelemetryProducerService
{
    /// <summary>Publish a pre-built telemetry value to its <see cref="TelemetryTopic.Path"/>.</summary>
    Task Publish(TelemetryTopic topic);

    #region Machine

    /// <summary>Publish the current machine state.</summary>
    Task PublishMachineState(MachineState value);

    /// <summary>Publish the total parts counter.</summary>
    Task PublishMachineParts(int value);

    /// <summary>Publish the total cycles counter.</summary>
    Task PublishMachineCycles(int value);

    /// <summary>Publish the total meters counter.</summary>
    Task PublishMachineMeters(int value);

    /// <summary>Publish the currently loaded program.</summary>
    Task PublishMachineProgram(string value);

    #endregion Machine

    #region Indexed groups (alerts + storage)

    /// <summary>Publish (or update) an error description for the given instance (e.g. error code).</summary>
    Task PublishErrorDescription(string instance, string value);

    /// <summary>Publish (or update) a warning description for the given instance.</summary>
    Task PublishWarningDescription(string instance, string value);

    /// <summary>Publish (or update) a maintenance description for the given instance.</summary>
    Task PublishMaintenanceDescription(string instance, string value);

    /// <summary>Publish (or update) an action description for the given instance.</summary>
    Task PublishActionDescription(string instance, string value);

    /// <summary>Publish a storage's current fill level.</summary>
    Task PublishStorageLevel(string instance, int value);

    /// <summary>Publish a storage's maximum capacity.</summary>
    Task PublishStorageCapacity(string instance, int value);

    /// <summary>Publish a storage's current material identifier.</summary>
    Task PublishStorageMaterial(string instance, string value);

    /// <summary>Remove an error entry (empty retained payload).</summary>
    Task RemoveErrorDescription(string instance);

    /// <summary>Remove a warning entry (empty retained payload).</summary>
    Task RemoveWarningDescription(string instance);

    /// <summary>Remove a maintenance entry (empty retained payload).</summary>
    Task RemoveMaintenanceDescription(string instance);

    /// <summary>Remove an action entry (empty retained payload).</summary>
    Task RemoveActionDescription(string instance);

    /// <summary>Remove a storage entry (clears its level, capacity and material topics).</summary>
    Task RemoveStorage(string instance);

    /// <summary>Publish a tool's description (name).</summary>
    Task PublishToolDescription(string instance, string value);

    /// <summary>Publish a tool's type.</summary>
    Task PublishToolType(string instance, ToolType value);

    /// <summary>Publish a tool's counter type.</summary>
    Task PublishToolCounterType(string instance, CounterType value);

    /// <summary>Publish a tool's current counter value.</summary>
    Task PublishToolCounter(string instance, int value);

    /// <summary>Remove a tool entry (clears its description, tool-type, counter-type and counter topics).</summary>
    Task RemoveTool(string instance);

    #endregion Indexed groups

    #region Current batch variant

    /// <summary>Publish the current batch variant id.</summary>
    Task PublishBatchVariantId(string value);

    /// <summary>Publish the current batch variant execution state.</summary>
    Task PublishBatchVariantState(BatchState value);

    /// <summary>Publish the current batch variant progress (0–100 %).</summary>
    Task PublishBatchVariantProgress(int value);

    /// <summary>Publish the meters processed for the current batch variant.</summary>
    Task PublishBatchVariantMeters(float value);

    /// <summary>Publish the current batch variant start timestamp.</summary>
    Task PublishBatchVariantStarted(DateTime value);

    /// <summary>Publish the current batch variant end timestamp.</summary>
    Task PublishBatchVariantFinished(DateTime value);

    #endregion Current batch variant
}
