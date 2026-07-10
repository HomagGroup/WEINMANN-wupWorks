using Wup.Works.SiteBroker.Client.Models;
using Wup.Works.SiteBroker.Client.Models.Enums;

namespace Wup.Works.SiteBroker.Client.Interfaces;

public interface ISiteBrokerControllerService
{
    /// <summary>
    /// The event handler handling a change of the online mode.
    /// </summary>
    public event EventHandler<OnlineModeStatusEventArgs>? OnlineModeChangedResponse;

    /// <summary>
    /// The event handler handling a response an order was loaded.
    /// </summary>
    public event EventHandler<OrderStatusEventArgs>? OrderLoadedResponse;

    /// <summary>
    /// The event handler handling a response an batch was prepared.
    /// </summary>
    public event EventHandler<BatchStatusEventArgs>? BatchPreparedResponse;

    /// <summary>
    /// The event handler handling a response an batch variant was executed.
    /// </summary>
    public event EventHandler<RunStatusEventArgs>? BatchVariantExecutedResponse;

    /// <summary>
    /// Connect to the site broker.
    /// </summary>
    /// <returns>An asynchronous task</returns>
    Task Connect();

    /// <summary>
    /// Disconnect from the site broker.
    /// </summary>
    /// <returns>An asynchronous task</returns>
    Task Disconnect();

    /// <summary>
    /// Send request when order should be loaded
    /// </summary>
    /// <param name="machineNumber">The machine number identifying the machine</param>
    /// <param name="orderId">The id of the order</param>
    /// <param name="fileName">The file name (legacy mode).</param>
    /// <returns>An asynchronous task</returns>
    Task SendLoadOrderRequest(string machineNumber, Guid orderId, string? fileName);

    /// <summary>
    /// Send request when batch prepare is requested.
    /// </summary>
    /// <param name="machineNumber">The machine number identifying the machine</param>
    /// <param name="batchId">The id of the batch</param>
    /// <param name="orderId">The id of the order (legacy mode).</param>
    /// <param name="variant">The variant (Non interactive mode).</param>
    /// <returns>An asynchronous task</returns>
    Task SendPrepareBatchRequest(string machineNumber, Guid batchId, Guid? orderId, string? variant);

    /// <summary>
    /// Send request when batch variant should be executed
    /// </summary>
    /// <param name="machineNumber">The machine number identifying the machine</param>
    /// <param name="batchVariantId">The id of the batch variant</param>
    /// <param name="batchId">The id of the batch (legacy mode).</param>
    /// <param name="orderId">The id of the order/document. Required for manual workstations that
    /// look the document up in their own database instead of the central database.</param>
    /// <returns>An asynchronous task</returns>
    Task SendExecuteBatchVariantRequest(string machineNumber, Guid batchVariantId, Guid? batchId, Guid? orderId = null);

    /// <summary>
    /// Clear response when order loaded received
    /// </summary>
    /// <param name="machineNumber">The machine number identifying the machine</param>
    /// <param name="orderId">The id of the order</param>
    /// <returns>An asynchronous task</returns>
    Task ClearOrderLoadedResponse(string machineNumber, Guid orderId);

    /// <summary>
    /// Clear response when batch prepared received
    /// </summary>
    /// <param name="machineNumber">The machine number identifying the machine</param>
    /// <param name="batchId">The id of the batch</param>
    /// <returns>An asynchronous task</returns>
    Task ClearBatchPreparedResponse(string machineNumber, Guid batchId);

    /// <summary>
    /// Clear response when batch variant started received
    /// </summary>
    /// <param name="machineNumber">The machine number identifying the machine</param>
    /// <param name="batchVariantId">The id of the batch variant</param>
    /// <returns>An asynchronous task</returns>
    Task ClearBatchVariantExecutedResponse(string machineNumber, Guid batchVariantId);
}