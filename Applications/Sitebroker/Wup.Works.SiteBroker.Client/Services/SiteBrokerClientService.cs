using Microsoft.Extensions.Options;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;
using Wup.Works.SiteBroker.Client.Configuration;
using Wup.Works.SiteBroker.Client.Helpers;
using Wup.Works.SiteBroker.Client.Interfaces;
using Wup.Works.SiteBroker.Client.Models;
using Wup.Works.SiteBroker.Client.Models.Enums;
using Wup.Works.SiteBroker.Client.Models.Payload;

namespace Wup.Works.SiteBroker.Client.Services;

public class SiteBrokerClientService : ISiteBrokerClientService, IDisposable
{
    private readonly IMqttClientService _mqttClientService;
    private readonly IOptions<SiteBrokerOptions> _options;

    private readonly string _orderLoadTopic;
    private readonly string _batchPrepareTopic;
    private readonly string _batchVariantProduceTopic;

    public event EventHandler<OrderStatusEventArgs> LoadOrderRequested;
    public event EventHandler<BatchStatusEventArgs> PrepareBatchRequested;
    public event EventHandler<RunStatusEventArgs> RunBatchVariantRequested;

    public SiteBrokerClientService(IMqttClientService mqttClientService, IOptions<SiteBrokerOptions> options)
    {
        _mqttClientService = mqttClientService;
        _options = options;

        _orderLoadTopic = TopicHelper.GetOrderLoadTopic(_options.Value.MachineNumber, Constants.Wildcard, Constants.Wildcard);
        _batchPrepareTopic = TopicHelper.GetBatchPrepareTopic(_options.Value.MachineNumber, Constants.Wildcard, Constants.Wildcard);
        _batchVariantProduceTopic = TopicHelper.GetBatchVariantProduceTopic(_options.Value.MachineNumber, Constants.Wildcard, Constants.Wildcard);
    }

    public async Task Connect()
    {
        await _mqttClientService.Connect();
        _mqttClientService.MessageReceived += MessageReceived;

        await _mqttClientService.Subscribe(_orderLoadTopic);
        await _mqttClientService.Subscribe(_batchPrepareTopic);
        await _mqttClientService.Subscribe(_batchVariantProduceTopic);
    }

    public async Task Disconnect()
    {
        await _mqttClientService.Unsubscribe(_orderLoadTopic);
        await _mqttClientService.Unsubscribe(_batchPrepareTopic);
        await _mqttClientService.Unsubscribe(_batchVariantProduceTopic);

        _mqttClientService.MessageReceived -= MessageReceived;
    }

    public async Task SendOrderLoadedResponse(Guid orderId, OrderStatus orderStatus)
    {
        var payload = new GenericPayloadDto()
        {
            Status = (int)orderStatus,
            AdditionalProperties = new Dictionary<string, string>()
            {
                { Constants.OrderId, orderId.ToString() }
            }
        };

        var orderLoadedTopic = TopicHelper.GetOrderLoadedTopic(_options.Value.MachineNumber, Constants.Orchestrator, orderId.ToString());
        await _mqttClientService.Publish(orderLoadedTopic, JsonSerializer.Serialize(payload));
    }

    public async Task SendBatchPreparedResponse(Guid batchId, Guid orderId, BatchStatus batchStatus)
    {
        var payload = new GenericPayloadDto()
        {
            Status = (int)batchStatus,
            AdditionalProperties = new Dictionary<string, string>()
            {
                { Constants.BatchId, batchId.ToString() },
                { Constants.OrderId, orderId.ToString() }
            }
        };

        var batchPreparedTopic = TopicHelper.GetBatchPreparedTopic(_options.Value.MachineNumber, Constants.Orchestrator, batchId.ToString());
        await _mqttClientService.Publish(batchPreparedTopic, JsonSerializer.Serialize(payload));
    }

    public async Task SendBatchVariantExecutedResponse(Guid batchVariantId, Guid batchId, Guid orderId, RunStatus runStatus)
    {
        var payload = new GenericPayloadDto()
        {
            Status = (int)runStatus,
            AdditionalProperties = new Dictionary<string, string>()
            {
                { Constants.BatchVariantId, batchVariantId.ToString() },
                { Constants.BatchId, batchId.ToString() },
                { Constants.OrderId, orderId.ToString() }
            }
        };

        var batchVariantProducedTopic = TopicHelper.GetBatchVariantProducedTopic(_options.Value.MachineNumber, Constants.Orchestrator, batchVariantId.ToString());
        await _mqttClientService.Publish(batchVariantProducedTopic, JsonSerializer.Serialize(payload));
    }

    public async Task SendOnlineModeResponse(bool onlineModeEnabled)
    {
        var payload = new GenericPayloadDto()
        {
            Status = (int)MapToOnlineModeStatus(onlineModeEnabled),
            AdditionalProperties = new Dictionary<string, string>()
        };

        var onlineModeTopic = TopicHelper.GetOnlineModeTopic(_options.Value.MachineNumber, Constants.Orchestrator);
        await _mqttClientService.Publish(onlineModeTopic, JsonSerializer.Serialize(payload));
    }

    public async Task ClearLoadOrderRequest(string orchestrator, Guid orderId)
    {
        var orderLoadedTopic = TopicHelper.GetOrderLoadTopic(_options.Value.MachineNumber, orchestrator, orderId.ToString());
        await _mqttClientService.Publish(orderLoadedTopic, string.Empty);
    }

    public async Task ClearPrepareBatchRequest(string orchestrator, Guid batchId)
    {
        var batchPreparedTopic = TopicHelper.GetBatchPrepareTopic(_options.Value.MachineNumber, orchestrator, batchId.ToString());
        await _mqttClientService.Publish(batchPreparedTopic, string.Empty);
    }

    public async Task ClearExecuteBatchVariantRequest(string orchestrator, Guid batchVariantId)
    {
        var batchVariantProducedTopic = TopicHelper.GetBatchVariantProduceTopic(_options.Value.MachineNumber, orchestrator, batchVariantId.ToString());
        await _mqttClientService.Publish(batchVariantProducedTopic, string.Empty);
    }

    public async void Dispose()
        => await Disconnect();

    private void MessageReceived(object? sender, MqttApplicationMessageReceivedEventArgs e)
    {
        var message = e.ApplicationMessage;
        var topic = message.Topic;
        var id = topic.Split("/")[3];
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

        if(string.IsNullOrEmpty(payload))
            return;

        if (_orderLoadTopic.ValidateTopic(topic))
        {
            var dict = GetAdditionalProperties(payload);
            dict.TryGetValue("Filename", out var filename);

            LoadOrderRequested?.Invoke(this, new OrderStatusEventArgs()
            {
                Filename = filename,
                OrderId = Guid.Parse(id)
            });
        }
        else if (_batchPrepareTopic.ValidateTopic(topic))
        {
            var dict = GetAdditionalProperties(payload);
            dict.TryGetValue("Variant", out var variant);
            dict.TryGetValue("OrderId", out var orderId);

            PrepareBatchRequested?.Invoke(this, new BatchStatusEventArgs()
            {
                BatchId = Guid.Parse(id),
                OrderId = orderId != null ? Guid.Parse(orderId) : null,
                Variant = variant
            });
        }
        else if (_batchVariantProduceTopic.ValidateTopic(topic))
        {
            var dict = GetAdditionalProperties(payload);
            dict.TryGetValue(Constants.BatchId, out var batchId);
            dict.TryGetValue(Constants.OrderId, out var orderId);

            RunBatchVariantRequested?.Invoke(this, new RunStatusEventArgs()
            {
                BatchVariantId = Guid.Parse(id),
                BatchId = batchId != null ? Guid.Parse(batchId) : null,
                OrderId = orderId != null ? Guid.Parse(orderId) : null
            });
        }
    }

    private IDictionary<string, string> GetAdditionalProperties(string payload)
    {
        var content = JsonSerializer.Deserialize<GenericPayloadDto>(payload);
        return content?.AdditionalProperties ?? new Dictionary<string, string>();
    }

    private static OnlineModeStatus MapToOnlineModeStatus(bool isOnlineMode)
    {
        return isOnlineMode ? OnlineModeStatus.Online : OnlineModeStatus.Offline;
    }
}
