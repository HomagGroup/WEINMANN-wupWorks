namespace Wup.Works.SiteBroker.Client.Models.Enums;

/// <summary>
/// Counter type of a tool, published as the numeric <c>value</c> of the
/// <c>{MachineNumber}/telemetry/machine/tool/{instance}/counter-type</c> topic.
/// </summary>
public enum CounterType
{
    /// <summary>Fastening shoots for fastening tools.</summary>
    Shoots = 1,

    /// <summary>Distance in meter, e.g. for milling, marking, sawing and glueing.</summary>
    Distance = 2,

    /// <summary>Hits, e.g. for drilling.</summary>
    Hits = 3
}
