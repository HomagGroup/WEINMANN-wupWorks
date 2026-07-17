using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Server;
using System.Text;
using Wup.Works.SiteBroker.Client.Configuration;
using Wup.Works.SiteBroker.Client.Interfaces;
using Wup.Works.SiteBroker.Client.Models;
using System.Collections.ObjectModel;

namespace Wup.Works.SiteBroker.Client.Services;

public class MqttClientService : IMqttClientService
{
    private readonly ILogger<IMqttClientService> _logger;
    private readonly IManagedMqttClient _managedMqttClient;
    private readonly MqttOptions _options;
    private MqttLastWillMessage? _lastWillMessage;
    private readonly Collection<string> _subscribedTopics = [];

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler<MqttApplicationMessageReceivedEventArgs>? MessageReceived;

    public AutoResetEvent ConnectedAutoResetEvent { get; }
    public virtual bool IsConnected => _managedMqttClient.IsConnected;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttClientService" /> class.
    /// </summary>
    /// <param name="mqttFactory">IMqttFactory instance.</param>
    /// <param name="options">MQTT options to configure the client.</param>
    /// <param name="logger">ILogger instance.</param>
    /// <param name="lastWillMessage">Specified last will message.</param>
    public MqttClientService(MqttFactory mqttFactory, MqttOptions options, ILogger<IMqttClientService> logger, MqttLastWillMessage? lastWillMessage)
    {
        ConnectedAutoResetEvent = new AutoResetEvent(false);
        _options = options;
        _logger = logger;
        _lastWillMessage = lastWillMessage;

        if (logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("[MQTT] Enabling debug mode for the mqtt connection");
            _managedMqttClient = mqttFactory.CreateManagedMqttClient(ConfigureLogging());
        }
        else
        {
            _logger.LogDebug("[MQTT] Starting without debug mode for the mqtt connection");
            _managedMqttClient = mqttFactory.CreateManagedMqttClient();
        }

        _managedMqttClient.ConnectedAsync += e =>
        {
            _logger.LogInformation("[MQTT] Client {id} connected", _options.Id);
            ConnectedAutoResetEvent.Set();
            Connected?.Invoke(this, e);

            return Task.CompletedTask;
        };

        _managedMqttClient.ConnectingFailedAsync += e =>
        {
            if (e.Exception != null)
            {
                _logger.LogError("[MQTT] Connecting client {id} failed: {exception}", _options.Id, e.Exception.Message);
            }

            return Task.CompletedTask;
        };

        _managedMqttClient.DisconnectedAsync += e =>
        {
            _logger.LogInformation("[MQTT] Client {id} disconnected", _options.Id);
            Disconnected?.Invoke(this, e);

            return Task.CompletedTask;
        };

        _managedMqttClient.ApplicationMessageReceivedAsync += e =>
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("[MQTT] Message received on topic {topic}", e.ApplicationMessage.Topic);
            }

            MessageReceived?.Invoke(this, e);

            return Task.CompletedTask;
        };

        _managedMqttClient.ApplicationMessageSkippedAsync += e =>
        {
            _logger.LogError("[MQTT] Message skipped because internal queue is full: {@message}", e.ApplicationMessage.ApplicationMessage);

            return Task.CompletedTask;
        };

        _managedMqttClient.ApplicationMessageProcessedAsync += e =>
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("[MQTT] Message processed on topic {topic}", e.ApplicationMessage.ApplicationMessage.Topic);
            }

            if (e.Exception != null)
            {
                _logger.LogError("[MQTT] Message processing failed: {exception}", e.Exception.Message);
            }

            return Task.CompletedTask;
        };
    }

    public async Task Connect()
    {
        if (IsConnected)
        {
            _logger.LogInformation("[MQTT] Client is already connected");
            // Invoke the connect event, to prevent infinite wait times.
            Connected?.Invoke(this, EventArgs.Empty);
            return;
        }

        MqttClientOptionsBuilder optionsBuilder;

        if (String.IsNullOrEmpty(_options.Username))
        {
            optionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(_options.Id)
                .WithTcpServer(_options.Hostname, _options.Port)
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .WithCleanSession(_options.CleanSession)
                .WithSessionExpiryInterval(_options.SessionExpiryInterval)
                .WithReceiveMaximum(_options.ReceiveMaximum);
        }
        else
        {
            optionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(_options.Id)
                .WithCredentials(_options.Username, Encoding.UTF8.GetString(Convert.FromBase64String(_options.Password)))
                .WithTcpServer(_options.Hostname, _options.Port)
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .WithCleanSession(_options.CleanSession)
                .WithSessionExpiryInterval(_options.SessionExpiryInterval)
                .WithReceiveMaximum(_options.ReceiveMaximum);
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("[MQTT] Client {id} connecting to {host} and port {port}", _options.Id, _options.Hostname, _options.Port);
            _logger.LogDebug("[MQTT] Client {id} using username {username}", _options.Id, _options.Username);
            if (_options.CleanSession)
                _logger.LogDebug("[MQTT] Client {id} using a clean session", _options.Id);
            else
                _logger.LogDebug("[MQTT] Client {id} preserves the session", _options.Id);
            _logger.LogDebug("[MQTT] Client {id} using session expiry interval {interval}", _options.Id, _options.SessionExpiryInterval);
            _logger.LogDebug("[MQTT] Client {id} using a connect timeout of {timeOut} seconds", _options.Id, _options.ConnectTimeoutInSeconds);
            _logger.LogDebug("[MQTT] Client {id} using a timeout for message reading of {timeOut} milliseconds", _options.Id,
                _options.ReadMessageTimeoutInMilliseconds);
            _logger.LogDebug("[MQTT] Client {id} using a maximum message queue length of {receiveMaximum}", _options.Id, _options.ReceiveMaximum);
        }

        MqttClientOptionsBuilder optionsBuilderWithLastWill;
        if (_lastWillMessage == default)
        {
            optionsBuilderWithLastWill = optionsBuilder;
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("[MQTT] Client {id} using default last will configuration", _options.Id);
            }
        }
        else
        {
            optionsBuilderWithLastWill = optionsBuilder
                .WithWillPayload(_lastWillMessage.Payload)
                .WithWillQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)_lastWillMessage.Qos)
                .WithWillRetain(_lastWillMessage.Retain)
                .WithWillTopic(_lastWillMessage.Topic);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("[MQTT] Client {id} using last will configuration: {@lastWillMessage}", _options.Id, _lastWillMessage);
            }
        }

        MqttClientOptions options = optionsBuilderWithLastWill.Build();

        try
        {
            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(_options.AutoReconnectDelay)
                .WithMaxPendingMessages(_options.MaxPendingMessages)
                .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
                .WithClientOptions(options)
                .Build();
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("[MQTT] Client {id} using reconnect delay {autoReconnectDelay}.", managedOptions.ClientOptions.ClientId,
                    managedOptions.AutoReconnectDelay);
                _logger.LogDebug("[MQTT] Client {id} using a maximum message queue length of {publishMaximum}", managedOptions.ClientOptions.ClientId,
                    managedOptions.MaxPendingMessages);
                _logger.LogDebug("[MQTT] Client {id} using pending messages overflow strategy '{overflowStrategy}'",
                    managedOptions.ClientOptions.ClientId, managedOptions.PendingMessagesOverflowStrategy);
            }

            await _managedMqttClient.StartAsync(managedOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("[MQTT] Failed to connect: {exception}", ex.Message);
            throw;
        }
    }

    public async Task Disconnect() => await _managedMqttClient.StopAsync();

    public async Task Publish(string topic, string payload, bool retainFlag, int qos) 
        => await Enqueue(new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
            .WithRetainFlag(retainFlag)
            .Build());

    public async Task Publish(string topic, string payload, bool retainFlag) 
        => await Publish(topic, payload, retainFlag, 2);

    /// <inheritdoc />
    public async Task Publish(string topic, string payload, int qos) 
        => await Publish(topic, payload, true, qos);

    /// <inheritdoc />
    public async Task Publish(string topic, string payload) 
        => await Publish(topic, payload, true, 2);

    public async Task SetLastWillMessage(MqttLastWillMessage lastWillMessage)
    {
        if (!IsConnected)
        {
            throw new MqttCommunicationException("[MQTT] Could not set last will message. Client is not connected");
        }

        _lastWillMessage = lastWillMessage;

        // Reconnect client to set new last will message
        await Disconnect();
        await Connect();
        if (!ConnectedAutoResetEvent.WaitOne(TimeSpan.FromSeconds(_options.ConnectTimeoutInSeconds)))
        {
            throw new MqttCommunicationException("[MQTT] Could not reconnect client after setting last will message.");
        }
    }

    public async Task Subscribe(string topic, int qos)
    {
        if (_subscribedTopics.Contains(topic))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("[MQTT] Already subscribed to topic {topic}", topic);
            }

            return;
        }

        await SubscribeIfConnected(topic, qos);
        _subscribedTopics.Add(topic);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("[MQTT] Subscribed to topic {topic}", topic);
        }
    }

    public async Task Subscribe(string topic) =>
        await Subscribe(topic, 2);

    public async Task Unsubscribe(string topic)
    {
        await UnsubscribeIfConnected(topic);
        _subscribedTopics.Remove(topic);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("[MQTT] Unsubscribed from topic {topic}", topic);
        }
    }

    public void Dispose()
    {
        Connected = null;
        Disconnected = null;
        MessageReceived = null;

        _subscribedTopics.Clear();
        ConnectedAutoResetEvent.Dispose();
        _managedMqttClient.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Configure the logging for the mqtt connection.
    /// </summary>
    /// <returns>The logger for the mqtt events.</returns>
    private MqttNetEventLogger ConfigureLogging()
    {
        var mqttEventLogger = new MqttNetEventLogger();

        mqttEventLogger.LogMessagePublished += (_, args) =>
        {
            var output = new StringBuilder($"[MQTT] [{args.LogMessage.Source}]: {args.LogMessage.Message}");

            if (args.LogMessage.Exception != null)
            {
                output.AppendLine(args.LogMessage.Exception.ToString());
            }

            switch (args.LogMessage.Level)
            {
                case MqttNetLogLevel.Error:
                    _logger.LogError("{mqttLogMessage}", output.ToString());
                    break;
                case MqttNetLogLevel.Warning:
                    _logger.LogWarning("{mqttLogMessage}", output.ToString());
                    break;
                case MqttNetLogLevel.Info:
                    _logger.LogInformation("{mqttLogMessage}", output.ToString());
                    break;
                default:
                    _logger.LogDebug("{mqttLogMessage}", output.ToString());
                    break;
            }
        };

        return mqttEventLogger;
    }

    /// <summary>
    /// Queues the stated message internally in the managed MQTT client.
    /// </summary>
    /// <param name="message">MQTT message to be published.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task Enqueue(MqttApplicationMessage message)
    {
        await _managedMqttClient.EnqueueAsync(message);

        if (!IsConnected)
        {
            _logger.LogWarning("[MQTT] Client is not connected. Message on topic {topic} queued. {count} messages pending", message.Topic,
                _managedMqttClient.PendingApplicationMessagesCount);
        }
    }

    private async Task SubscribeIfConnected(string topic, int qos)
    {
        if (!IsConnected)
        {
            throw new MqttCommunicationException($"[MQTT] Could not subscribe to topic {topic}. Client is not connected");
        }

        await _managedMqttClient.SubscribeAsync(
        [
            new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                .Build()
        ]);
    }

    private async Task UnsubscribeIfConnected(string topic)
    {
        if (!IsConnected)
        {
            throw new MqttCommunicationException($"[MQTT] Could not unsubscribe from topic {topic}. Client is not connected");
        }

        await _managedMqttClient.UnsubscribeAsync(topic);
    }
}
