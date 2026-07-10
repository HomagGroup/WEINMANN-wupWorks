namespace Wup.Works.SiteBroker.Client.Models.Enums;

/// <summary>
/// Machine state values, published as the numeric <c>value</c> of the
/// <c>{MachineNumber}/data/machine/state</c> signal. The five values match the
/// HOMAG MMR OPC-UA interface exactly.
/// </summary>
public enum MachineState
{
    /// <summary>Machine off / not producing.</summary>
    Off = 1,

    /// <summary>Powered, waiting (no job running).</summary>
    Idle = 2,

    /// <summary>Production running.</summary>
    Working = 3,

    /// <summary>Fault / stopped on error.</summary>
    Error = 4,

    /// <summary>Test / commissioning mode.</summary>
    Test = 5
}
