using Wup.Works.SiteBroker.Client.Models.Enums;

namespace Wup.Works.SiteBroker.Client.Models;

public class BatchStatusEventArgs : EventArgs
{
    public Guid BatchId { get; set; }

    public Guid? OrderId { get; set; }

    public string? Variant { get; set; }

    public BatchStatus Status { get; set; }
}
