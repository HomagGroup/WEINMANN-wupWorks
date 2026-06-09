namespace Wup.Works.SiteBroker.Client.Configuration;

/// <summary>
/// Configuration model for the site broker client.
/// </summary>
public sealed class SiteBrokerOptions
{
    /// <summary>
    /// The machine number
    /// </summary>
    public string MachineNumber { get; set; }

    /// <summary>
    /// Hostname of the broker and central server.
    /// </summary>
    public string Hostname { get; set; }

}