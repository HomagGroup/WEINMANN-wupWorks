using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Wup.Works.SiteBroker.Client.Configuration;
using Wup.Works.SiteBroker.Client.Helpers;
using Wup.Works.SiteBroker.Client.Interfaces;
using Wup.Works.SiteBroker.Client.Models;
using Wup.Works.SiteBroker.Client.Models.Enums;
using Wup.Works.SiteBroker.Client.Models.Payload;
using Wup.Works.SiteBroker.Client.Services;
using Wup.Works.SiteBroker.Client.Services.BackgroundWorkers;

namespace Wup.Works.SiteBroker.Client;

public static class ServiceExtensions
{
    public static IServiceCollection AddSiteBroker(this IServiceCollection services, IConfiguration configuration, bool useLastWill = false)
    {
        var options = new SiteBrokerOptions();
        configuration.GetSection("SiteBrokerOptions").Bind(options);
        var siteBrokerOptions = Options.Create(options);

        services.AddHttpClient("SiteBrokerClient")
            .ConfigureHttpClient((_, client) => { client.Timeout = TimeSpan.FromSeconds(Constants.HttpTimeoutSeconds); });

        MqttLastWillMessage? lastWillMessage = null;

        if (useLastWill)
        {
            // Last Will: OnlineMode = Offline setzen, wenn die Verbindung unerwartet abbricht
            var onlineModeTopic = OrderTopicHelper.GetOnlineModeTopic(options.MachineNumber, Constants.Orchestrator);
            
            var offlinePayload = new GenericPayloadDto
            {
                Status = (int)OnlineModeStatus.Offline,
                AdditionalProperties = new Dictionary<string, string>()
            };

            lastWillMessage = new MqttLastWillMessage
            {
                Topic = onlineModeTopic,
                Payload = JsonSerializer.Serialize(offlinePayload),
                Qos = MqttQualityOfServiceLevel.ExactlyOnce,
                Retain = true
            };
        }

        services.AddConnectedMqttClient(configuration, siteBrokerOptions, lastWillMessage);
        services.AddSingleton<ISiteBrokerClientService, SiteBrokerClientService>(provider =>
        {
            var mqttClient = provider.GetRequiredService<IMqttClientService>();
            var siteBrokerClientService = new SiteBrokerClientService(mqttClient, siteBrokerOptions);

            return siteBrokerClientService;
        });
    

        services.AddSingleton<ISiteBrokerControllerService, SiteBrokerControllerService>(provider =>
        {
            var mqttClient = provider.GetRequiredService<IMqttClientService>();
            var siteBrokerControllerService = new SiteBrokerControllerService(mqttClient);

            return siteBrokerControllerService;
        });

        services.AddSingleton<ITelemetryProducerService, TelemetryProducerService>(provider =>
        {
            var mqttClient = provider.GetRequiredService<IMqttClientService>();

            return new TelemetryProducerService(mqttClient, siteBrokerOptions);
        });

        services.AddSingleton<ITelemetryConsumerService, TelemetryConsumerService>(provider =>
        {
            var mqttClient = provider.GetRequiredService<IMqttClientService>();

            return new TelemetryConsumerService(mqttClient, siteBrokerOptions);
        });

        services.AddHostedService<SiteBrokerWorker>();

        return services;
    }

    private static IServiceCollection AddConnectedMqttClient(this IServiceCollection services, 
        IConfiguration configuration,
        IOptions<SiteBrokerOptions> siteBrokerOptions,
        MqttLastWillMessage? lastWillMessage = null)
    {
        services.AddSingleton<MqttFactory, MqttFactory>();

        var options = new MqttOptions();
        configuration.GetSection("Mqtt").Bind(options);
        options.Hostname = siteBrokerOptions.Value.Hostname;

        #region Validation

        if (options.ConnectTimeoutInSeconds is < 30 or > 600)
        {
            throw new ValidationException($"[MQTT] The defined timeout of {options.ConnectTimeoutInSeconds} seconds for the mqtt connection is outside of the allowed range.");
        }

        #endregion Validation

        return services.AddSingleton<IMqttClientService>(provider =>
        {
            var mqttClient = new MqttClientService(
                provider.GetRequiredService<MqttFactory>(),
                options,
                provider.GetRequiredService<ILogger<IMqttClientService>>(),
                lastWillMessage);

            mqttClient.Connect().GetAwaiter().GetResult();
            mqttClient.ConnectedAutoResetEvent.WaitOne(TimeSpan.FromSeconds(options.ConnectTimeoutInSeconds));

            return mqttClient;
        });
    }
}