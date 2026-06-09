using Wup.Works.SiteBroker.Client.Models;
using Wup.Works.SiteBroker.Client.Models.Enums;

namespace Wup.Works.SiteBroker.Client.Interfaces;

public interface ISiteBrokerClientService
{
    /// <summary>
    /// The event handler handling a request to load an order.
    /// </summary>
    public event EventHandler<OrderStatusEventArgs> LoadOrderRequested;

    /// <summary>
    /// The event handler handling a request to prepare a batch.
    /// </summary>
    public event EventHandler<BatchStatusEventArgs> PrepareBatchRequested;

    /// <summary>
    /// The event handler handling a request to run a batch variant.
    /// </summary>
    public event EventHandler<RunStatusEventArgs> RunBatchVariantRequested;

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
    /// Send response when order loaded
    /// </summary>
    /// <param name="orderId">The id of the order</param>
    /// <param name="orderStatus">The order status.</param>
    /// <returns>An asynchronous task</returns>
    Task SendOrderLoadedResponse(Guid orderId, OrderStatus orderStatus);

    /// <summary>
    /// Send response when batch prepared
    /// </summary>
    /// <param name="batchId">The id of the batch</param>
    /// <param name="orderId">The id of the order</param>
    /// <param name="batchStatus">The batch status.</param>
    /// <returns>An asynchronous task</returns>
    Task SendBatchPreparedResponse(Guid batchId, Guid orderId, BatchStatus batchStatus);

    /// <summary>
    /// Send response when batch variant executed
    /// </summary>
    /// <param name="batchVariantId">The id of the batch variant</param>
    /// <param name="batchId">The id of the batch</param>
    /// <param name="orderId">The id of the order</param>
    /// <param name="runStatus">The run status.</param>
    /// <returns>An asynchronous task</returns>
    Task SendBatchVariantExecutedResponse(Guid batchVariantId, Guid batchId, Guid orderId, RunStatus runStatus);

    /// <summary>
    /// Clear request when order should was loaded
    /// </summary>
    /// <param name="orchestrator">The sender of the message</param>
    /// <param name="orderId">The id of the order</param>
    /// <returns>An asynchronous task</returns>
    Task ClearLoadOrderRequest(string orchestrator, Guid orderId);

    /// <summary>
    /// Clear request when batch prepare was requested.
    /// </summary>
    /// <param name="orchestrator">The sender of the message</param>
    /// <param name="batchId">The id of the batch</param>
    /// <returns>An asynchronous task</returns>
    Task ClearPrepareBatchRequest(string orchestrator, Guid batchId);

    /// <summary>
    /// Clear request when batch variant should was started
    /// </summary>
    /// <param name="orchestrator">The sender of the message</param>
    /// <param name="batchVariantId">The id of the batch variant</param>
    /// <returns>An asynchronous task</returns>
    Task ClearExecuteBatchVariantRequest(string orchestrator, Guid batchVariantId);

    /// <summary>
    /// Send response when online mode chances
    /// </summary>
    /// <param name="onlineModeEnabled">Status of the online mode</param>
    /// <returns>An asynchronous task</returns>
    Task SendOnlineModeResponse(bool onlineModeEnabled);

    // Hint: Things to add later.
    // - Material interface
}