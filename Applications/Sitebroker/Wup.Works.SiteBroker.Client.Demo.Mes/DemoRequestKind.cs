namespace Wup.Works.SiteBroker.Client.Demo.Mes;

/// <summary>
/// The kind of command the MES demo can send to a machine.
/// </summary>
internal enum DemoRequestKind
{
    Order,
    Batch,
    BatchVariant
}
