namespace Wup.Works.SiteBroker.Client.Models.Enums;

/// <summary>
/// Execution state of the current batch, published as the numeric <c>value</c> of the
/// <c>{sender}/telemetry/order/batch/current/state</c> topic. Values match the command
/// interface's <see cref="RunStatus"/> (Inactive..Aborted).
/// </summary>
public enum BatchState
{
    /// <summary>Not started or paused.</summary>
    Inactive = 1,

    /// <summary>Currently producing.</summary>
    Active = 2,

    /// <summary>Finished successfully.</summary>
    Done = 3,

    /// <summary>Stopped before completion.</summary>
    Aborted = 4
}
