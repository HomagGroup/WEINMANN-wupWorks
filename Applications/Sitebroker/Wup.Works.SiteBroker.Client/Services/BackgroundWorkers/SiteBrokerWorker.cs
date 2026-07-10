using Microsoft.Extensions.Hosting;
using Wup.Works.SiteBroker.Client.Interfaces;

namespace Wup.Works.SiteBroker.Client.Services.BackgroundWorkers;

public class SiteBrokerWorker : BackgroundService
{
    private readonly ISiteBrokerClientService _siteBrokerClientService;
    private readonly ISiteBrokerControllerService _siteBrokerControllerService;
    private readonly ITelemetryConsumerService _telemetryConsumerService;

    public SiteBrokerWorker(
        ISiteBrokerClientService siteBrokerClientService,
        ISiteBrokerControllerService siteBrokerControllerService,
        ITelemetryConsumerService telemetryConsumerService)
    {
        _siteBrokerClientService = siteBrokerClientService;
        _siteBrokerControllerService = siteBrokerControllerService;
        _telemetryConsumerService = telemetryConsumerService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _siteBrokerClientService.Connect();
        await _siteBrokerControllerService.Connect();
        await _telemetryConsumerService.Connect();
    }
}