using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;
using Wup.Works.SiteBroker.Client.Configuration;
using Wup.Works.SiteBroker.Client.Helpers;
using Wup.Works.SiteBroker.Client.Interfaces;
using Wup.Works.SiteBroker.Client.Models;
using Wup.Works.SiteBroker.Client.Models.Enums;

namespace Wup.Works.SiteBroker.Client.Services;

/// <inheritdoc cref="ITelemetryProducerService" />
public class TelemetryProducerService : ITelemetryProducerService
{
    #region Topic keys

    private const string KeyState = "state";
    private const string KeyParts = "parts";
    private const string KeyCycles = "cycles";
    private const string KeyMeter = "meter";
    private const string KeyProgram = "program";
    private const string KeyDescription = "description";
    private const string KeyLevel = "level";
    private const string KeyCapacity = "capacity";
    private const string KeyMaterial = "material";
    private const string KeyToolType = "tool-type";
    private const string KeyCounterType = "counter-type";
    private const string KeyCounter = "counter";
    private const string KeyTime = "time";
    private const string KeyId = "id";
    private const string KeyProgress = "progress";
    private const string KeyStarted = "started";
    private const string KeyFinished = "finished";

    #endregion Topic keys

    private readonly IMqttClientService _mqttClientService;
    private readonly string _sender;

    public TelemetryProducerService(IMqttClientService mqttClientService, IOptions<SiteBrokerOptions> options)
    {
        _mqttClientService = mqttClientService;
        _sender = options.Value.MachineNumber;
    }

    public Task Publish(TelemetryTopic topic)
        => PublishValue(topic, topic.Payload.Value, topic.Payload.Type);

    #region Machine

    public Task PublishMachineState(MachineState value)
        => PublishValue(TelemetryTopicHelper.CreateMachineTelemetryTopic(_sender, KeyState), Number((int)value), TelemetryValueType.Number);

    public Task PublishMachineParts(int value)
        => PublishValue(TelemetryTopicHelper.CreateMachineTelemetryTopic(_sender, KeyParts), Number(value), TelemetryValueType.Number);

    public Task PublishMachineCycles(int value)
        => PublishValue(TelemetryTopicHelper.CreateMachineTelemetryTopic(_sender, KeyCycles), Number(value), TelemetryValueType.Number);

    public Task PublishMachineMeters(int value)
        => PublishValue(TelemetryTopicHelper.CreateMachineTelemetryTopic(_sender, KeyMeter), Number(value), TelemetryValueType.Number);

    public Task PublishMachineProgram(string value)
        => PublishValue(TelemetryTopicHelper.CreateMachineTelemetryTopic(_sender, KeyProgram), value, TelemetryValueType.String);

    #endregion Machine

    #region Indexed groups (messages, storage, tools)

    public Task PublishErrorDescription(string instance, string value)
        => PublishValue(TelemetryTopicHelper.CreateErrorTelemetryTopic(_sender, instance, KeyDescription), value, TelemetryValueType.String);

    public Task PublishWarningDescription(string instance, string value)
        => PublishValue(TelemetryTopicHelper.CreateWarningTelemetryTopic(_sender, instance, KeyDescription), value, TelemetryValueType.String);

    public Task PublishMaintenanceDescription(string instance, string value)
        => PublishValue(TelemetryTopicHelper.CreateMaintenanceTelemetryTopic(_sender, instance, KeyDescription), value, TelemetryValueType.String);

    public Task PublishActionDescription(string instance, string value)
        => PublishValue(TelemetryTopicHelper.CreateActionTelemetryTopic(_sender, instance, KeyDescription), value, TelemetryValueType.String);

    public Task PublishStorageLevel(string instance, int value)
        => PublishValue(TelemetryTopicHelper.CreateStorageTelemetryTopic(_sender, instance, KeyLevel), Number(value), TelemetryValueType.Number);

    public Task PublishStorageCapacity(string instance, int value)
        => PublishValue(TelemetryTopicHelper.CreateStorageTelemetryTopic(_sender, instance, KeyCapacity), Number(value), TelemetryValueType.Number);

    public Task PublishStorageMaterial(string instance, string value)
        => PublishValue(TelemetryTopicHelper.CreateStorageTelemetryTopic(_sender, instance, KeyMaterial), value, TelemetryValueType.String);

    public Task RemoveErrorDescription(string instance)
        => Remove(TelemetryTopicHelper.CreateErrorTelemetryTopic(_sender, instance, KeyDescription));

    public Task RemoveWarningDescription(string instance)
        => Remove(TelemetryTopicHelper.CreateWarningTelemetryTopic(_sender, instance, KeyDescription));

    public Task RemoveMaintenanceDescription(string instance)
        => Remove(TelemetryTopicHelper.CreateMaintenanceTelemetryTopic(_sender, instance, KeyDescription));

    public Task RemoveActionDescription(string instance)
        => Remove(TelemetryTopicHelper.CreateActionTelemetryTopic(_sender, instance, KeyDescription));

    public async Task RemoveStorage(string instance)
    {
        await Remove(TelemetryTopicHelper.CreateStorageTelemetryTopic(_sender, instance, KeyLevel));
        await Remove(TelemetryTopicHelper.CreateStorageTelemetryTopic(_sender, instance, KeyCapacity));
        await Remove(TelemetryTopicHelper.CreateStorageTelemetryTopic(_sender, instance, KeyMaterial));
    }

    public Task PublishToolDescription(string instance, string value)
        => PublishValue(TelemetryTopicHelper.CreateToolTelemetryTopic(_sender, instance, KeyDescription), value, TelemetryValueType.String);

    public Task PublishToolType(string instance, ToolType value)
        => PublishValue(TelemetryTopicHelper.CreateToolTelemetryTopic(_sender, instance, KeyToolType), Number((int)value), TelemetryValueType.Number);

    public Task PublishToolCounterType(string instance, CounterType value)
        => PublishValue(TelemetryTopicHelper.CreateToolTelemetryTopic(_sender, instance, KeyCounterType), Number((int)value), TelemetryValueType.Number);

    public Task PublishToolCounter(string instance, int value)
        => PublishValue(TelemetryTopicHelper.CreateToolTelemetryTopic(_sender, instance, KeyCounter), Number(value), TelemetryValueType.Number);

    public Task PublishToolTime(string instance, int value)
        => PublishValue(TelemetryTopicHelper.CreateToolTelemetryTopic(_sender, instance, KeyTime), Number(value), TelemetryValueType.Number);

    public async Task RemoveTool(string instance)
    {
        await Remove(TelemetryTopicHelper.CreateToolTelemetryTopic(_sender, instance, KeyDescription));
        await Remove(TelemetryTopicHelper.CreateToolTelemetryTopic(_sender, instance, KeyToolType));
        await Remove(TelemetryTopicHelper.CreateToolTelemetryTopic(_sender, instance, KeyCounterType));
        await Remove(TelemetryTopicHelper.CreateToolTelemetryTopic(_sender, instance, KeyCounter));
        await Remove(TelemetryTopicHelper.CreateToolTelemetryTopic(_sender, instance, KeyTime));
    }

    #endregion Indexed groups

    #region Current batch variant

    public Task PublishBatchVariantId(string value)
        => PublishValue(TelemetryTopicHelper.CreateBatchVariantTelemetryTopic(_sender, KeyId), value, TelemetryValueType.String);

    public Task PublishBatchVariantState(BatchState value)
        => PublishValue(TelemetryTopicHelper.CreateBatchVariantTelemetryTopic(_sender, KeyState), Number((int)value), TelemetryValueType.Number);

    public Task PublishBatchVariantProgress(int value)
        => PublishValue(TelemetryTopicHelper.CreateBatchVariantTelemetryTopic(_sender, KeyProgress), Number(value), TelemetryValueType.Number);

    public Task PublishBatchVariantMeters(float value)
        => PublishValue(TelemetryTopicHelper.CreateBatchVariantTelemetryTopic(_sender, KeyMeter), value.ToString(CultureInfo.InvariantCulture), TelemetryValueType.Number);

    public Task PublishBatchVariantStarted(DateTime value)
        => PublishValue(TelemetryTopicHelper.CreateBatchVariantTelemetryTopic(_sender, KeyStarted), Timestamp(value), TelemetryValueType.String);

    public Task PublishBatchVariantFinished(DateTime value)
        => PublishValue(TelemetryTopicHelper.CreateBatchVariantTelemetryTopic(_sender, KeyFinished), Timestamp(value), TelemetryValueType.String);

    #endregion Current batch variant

    private Task PublishValue(TelemetryTopic topic, string value, TelemetryValueType type)
    {
        topic.Payload.Type = type;
        topic.Payload.Value = value;
        topic.Payload.TimestampUtc = DateTime.UtcNow;

        return _mqttClientService.Publish(topic.Path, JsonSerializer.Serialize(topic.Payload, TelemetrySerialization.Options));
    }

    private Task Remove(TelemetryTopic topic)
        => _mqttClientService.Publish(topic.Path, string.Empty);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Timestamp(DateTime value)
        => value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
}
