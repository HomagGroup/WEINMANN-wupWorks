namespace Wup.Works.SiteBroker.Client.Demo.Machine;

/// <summary>
/// The kind of command the machine demo can receive from the orchestrator.
/// </summary>
internal enum DemoRequestKind
{
    Order,
    Batch,
    BatchVariant
}
