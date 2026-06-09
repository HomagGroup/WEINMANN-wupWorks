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

public class SiteBrokerControllerService : ISiteBrokerControllerService, IDisposable
{
    private readonly IMqttClientService _mqttClientService;

    private readonly string _orderLoadedTopic;
    private readonly string _batchPreparedTopic;
    private readonly string _batchVariantProducedTopic;
    private readonly string _onlineModeChangedTopic;

    public event EventHandler<OnlineModeStatusEventArgs> OnlineModeChangedResponse;
    public event EventHandler<OrderStatusEventArgs> OrderLoadedResponse;
    public event EventHandler<BatchStatusEventArgs> BatchPreparedResponse;
    public event EventHandler<RunStatusEventArgs> BatchVariantExecutedResponse;

    public SiteBrokerControllerService(IMqttClientService mqttClientService)
    {
        _mqttClientService = mqttClientService;

        _orderLoadedTopic = TopicHelper.GetOrderLoadedTopic(Constants.Wildcard, Constants.Orchestrator, Constants.Wildcard);
        _batchPreparedTopic = TopicHelper.GetBatchPreparedTopic(Constants.Wildcard, Constants.Orchestrator, Constants.Wildcard);
        _batchVariantProducedTopic = TopicHelper.GetBatchVariantProducedTopic(Constants.Wildcard, Constants.Orchestrator, Constants.Wildcard);
        _onlineModeChangedTopic = TopicHelper.GetOnlineModeTopic(Constants.Wildcard, Constants.Orchestrator);
    }

    public async Task Connect()
    {
        await _mqttClientService.Connect();
        _mqttClientService.MessageReceived += MessageReceived;

        await _mqttClientService.Subscribe(_orderLoadedTopic);
        await _mqttClientService.Subscribe(_batchPreparedTopic);
        await _mqttClientService.Subscribe(_batchVariantProducedTopic);
        await _mqttClientService.Subscribe(_onlineModeChangedTopic);
    }

    public async Task Disconnect()
    {
        await _mqttClientService.Unsubscribe(_orderLoadedTopic);
        await _mqttClientService.Unsubscribe(_batchPreparedTopic);
        await _mqttClientService.Unsubscribe(_batchVariantProducedTopic);
        await _mqttClientService.Unsubscribe(_onlineModeChangedTopic);

        _mqttClientService.MessageReceived -= MessageReceived;
    }

    public async Task SendLoadOrderRequest(string machineNumber, Guid orderId, string? fileName)
    {
        var payload = new GenericPayloadDto()
        {
            Status = 0,
            AdditionalProperties = new Dictionary<string, string>()
            {
                { Constants.OrderId, orderId.ToString() }
            }
        };

        if(fileName != null)
            payload.AdditionalProperties.Add(Constants.Filename, fileName);

        var loadOrderTopic = TopicHelper.GetOrderLoadTopic(machineNumber, Constants.Orchestrator, orderId.ToString());
        await _mqttClientService.Publish(loadOrderTopic, JsonSerializer.Serialize(payload));
    }

    public async Task SendPrepareBatchRequest(string machineNumber, Guid batchId, Guid? orderId, string? variant)
    {
        var payload = new GenericPayloadDto()
        {
            Status = 0,
            AdditionalProperties = new Dictionary<string, string>()
            {
                { Constants.BatchId, batchId.ToString() }
            }
        };

        if (orderId != null)
            payload.AdditionalProperties.Add(Constants.OrderId, orderId.ToString());

        if (variant != null)
            payload.AdditionalProperties.Add(Constants.Variant, variant);

        var prepareBatchTopic = TopicHelper.GetBatchPrepareTopic(machineNumber, Constants.Orchestrator, batchId.ToString());
        await _mqttClientService.Publish(prepareBatchTopic, JsonSerializer.Serialize(payload));
    }

    public async Task SendExecuteBatchVariantRequest(string machineNumber, Guid batchVariantId, Guid? batchId, Guid? orderId = null)
    {
        var payload = new GenericPayloadDto()
        {
            Status = 0,
            AdditionalProperties = new Dictionary<string, string>()
            {
                { Constants.BatchVariantId, batchVariantId.ToString() }
            }
        };

        if (batchId != null)
            payload.AdditionalProperties.Add(Constants.BatchId, batchId.ToString());

        if (orderId != null)
            payload.AdditionalProperties.Add(Constants.OrderId, orderId.ToString());

        var produceBatchVariantTopic = TopicHelper.GetBatchVariantProduceTopic(machineNumber, Constants.Orchestrator, batchVariantId.ToString());
        await _mqttClientService.Publish(produceBatchVariantTopic, JsonSerializer.Serialize(payload));
    }

    public async Task ClearOrderLoadedResponse(string machineNumber, Guid orderId)
    {
        var loadOrderTopic = TopicHelper.GetOrderLoadedTopic(machineNumber, Constants.Orchestrator, orderId.ToString());
        await _mqttClientService.Publish(loadOrderTopic, string.Empty);
    }

    public async Task ClearBatchPreparedResponse(string machineNumber, Guid batchId)
    {
        var prepareBatchTopic = TopicHelper.GetBatchPreparedTopic(machineNumber, Constants.Orchestrator, batchId.ToString());
        await _mqttClientService.Publish(prepareBatchTopic, string.Empty);
    }

    public async Task ClearBatchVariantExecutedResponse(string machineNumber, Guid batchVariantId)
    {
        var produceBatchVariantTopic = TopicHelper.GetBatchVariantProducedTopic(machineNumber, Constants.Orchestrator, batchVariantId.ToString());
        await _mqttClientService.Publish(produceBatchVariantTopic, string.Empty);
    }

    public async void Dispose()
        => await Disconnect();

    private void MessageReceived(object? sender, MqttApplicationMessageReceivedEventArgs e)
    {
        var message = e.ApplicationMessage;
        var topic = message.Topic;
        var topicParts = topic.Split('/');
        var machineNumber = topicParts[0];
        var id = topicParts[3];
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

        if(string.IsNullOrEmpty(payload))
            return;

        if (string.IsNullOrEmpty(payload))
        {
            return;
        }

        if (_orderLoadedTopic.ValidateTopic(topic))
        {
            var dict = GetAdditionalProperties(payload);
            dict.TryGetValue("Filename", out var filename);

            OrderLoadedResponse?.Invoke(this, new OrderStatusEventArgs()
            {
                Filename = filename,
                OrderId = Guid.Parse(id),
                Status = (OrderStatus)GetPayload(payload).Status
            });
        }
        else if (_batchPreparedTopic.ValidateTopic(topic))
        {
            var dict = GetAdditionalProperties(payload);
            dict.TryGetValue("Variant", out var variant);
            dict.TryGetValue("OrderId", out var orderId);

            BatchPreparedResponse?.Invoke(this, new BatchStatusEventArgs()
            {
                BatchId = Guid.Parse(id),
                OrderId = orderId != null ? Guid.Parse(orderId) : null,
                Variant = variant,
                Status = (BatchStatus)GetPayload(payload).Status
            });
        }
        else if (_batchVariantProducedTopic.ValidateTopic(topic))
        {
            var dict = GetAdditionalProperties(payload);
            dict.TryGetValue(Constants.BatchId, out var batchId);
            dict.TryGetValue(Constants.OrderId, out var orderId);

            BatchVariantExecutedResponse?.Invoke(this, new RunStatusEventArgs()
            {
                BatchVariantId = Guid.Parse(id),
                BatchId = batchId != null ? Guid.Parse(batchId) : null,
                OrderId = orderId != null ? Guid.Parse(orderId) : null,
                Status = (RunStatus)GetPayload(payload).Status
            });
        }
        else if (_onlineModeChangedTopic.ValidateTopic(topic))
        {
            OnlineModeChangedResponse?.Invoke(this, new OnlineModeStatusEventArgs()
            {
                MachineNumber = machineNumber,
                Status = GetPayload(payload).Status == 2 ? OnlineModeStatus.Online : OnlineModeStatus.Offline
            });
        }
    }

    private IDictionary<string, string> GetAdditionalProperties(string payload)
    {
        var content = GetPayload(payload);
        return content?.AdditionalProperties ?? new Dictionary<string, string>();
    }

    private GenericPayloadDto GetPayload(string payload)
    {
        return JsonSerializer.Deserialize<GenericPayloadDto>(payload) ?? new GenericPayloadDto();
    }
}



