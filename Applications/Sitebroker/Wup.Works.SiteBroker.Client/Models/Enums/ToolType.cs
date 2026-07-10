namespace Wup.Works.SiteBroker.Client.Models.Enums;

/// <summary>
/// Type of a tool, published as the numeric <c>value</c> of the
/// <c>{MachineNumber}/telemetry/machine/tool/{instance}/tool-type</c> topic.
/// </summary>
public enum ToolType
{
    /// <summary>Milling tool.</summary>
    Milling = 1,

    /// <summary>Drilling tool.</summary>
    Drilling = 2,

    /// <summary>Marking tool.</summary>
    Marking = 3,

    /// <summary>Fastening tool.</summary>
    Fastening = 4,

    /// <summary>Sawing tool.</summary>
    Sawing = 5,

    /// <summary>Glueing tool.</summary>
    Glueing = 6
}
