namespace Wup.Works.SiteBroker.Client.Configuration;

/// <summary>
/// Configuration model for the site broker client.
/// </summary>
public sealed class SiteBrokerOptions
{
    /// <summary>
    /// The machine number
    /// </summary>
    public string MachineNumber { get; set; } = string.Empty;

    /// <summary>
    /// Hostname of the broker and central server.
    /// </summary>
    public string Hostname { get; set; } = string.Empty;

    /// <summary>
    /// When <c>true</c> (default), the data consumer subscribes to the data of all machines
    /// (<c>+/data/#</c>). When <c>false</c>, it only subscribes to the data of
    /// <see cref="MachineNumber"/>.
    /// </summary>
    public bool SubscribeToAllMachinesData { get; set; } = true;

}