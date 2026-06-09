using Wup.Works.SiteBroker.Client.Models.Enums;

namespace Wup.Works.SiteBroker.Client.Models;

public class OrderStatusEventArgs : EventArgs
{
    public Guid OrderId { get; set; }

    public string? Filename { get; set; }

    public OrderStatus Status { get; set; }
}
