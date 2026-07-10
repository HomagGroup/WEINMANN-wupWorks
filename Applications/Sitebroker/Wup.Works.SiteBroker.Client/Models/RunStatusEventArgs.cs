using Wup.Works.SiteBroker.Client.Models.Enums;

namespace Wup.Works.SiteBroker.Client.Models;

public class RunStatusEventArgs : EventArgs
{
    public Guid BatchVariantId { get; set; }

    public Guid? BatchId { get; set; }

    public Guid? OrderId { get; set; }

    public RunStatus Status { get; set; }
}
