using Wup.Works.SiteBroker.Client.Models.Enums;

namespace Wup.Works.SiteBroker.Client.Models;

public class OnlineModeStatusEventArgs : EventArgs
{
    public string? MachineNumber { get; set; }
    public OnlineModeStatus Status { get; set; }
}
