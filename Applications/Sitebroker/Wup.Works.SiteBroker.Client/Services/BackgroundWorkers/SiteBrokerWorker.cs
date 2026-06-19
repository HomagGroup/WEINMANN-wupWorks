using Microsoft.Extensions.Hosting;
using Wup.Works.SiteBroker.Client.Interfaces;

namespace Wup.Works.SiteBroker.Client.Services.BackgroundWorkers;

public class SiteBrokerWorker : BackgroundService
{
    private readonly ISiteBrokerClientService _siteBrokerClientService;
    private readonly ISiteBrokerControllerService _siteBrokerControllerService;
    private readonly ISiteBrokerDataConsumer _siteBrokerDataConsumer;

    public SiteBrokerWorker(
        ISiteBrokerClientService siteBrokerClientService,
        ISiteBrokerControllerService siteBrokerControllerService,
        ISiteBrokerDataConsumer siteBrokerDataConsumer)
    {
        _siteBrokerClientService = siteBrokerClientService;
        _siteBrokerControllerService = siteBrokerControllerService;
        _siteBrokerDataConsumer = siteBrokerDataConsumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _siteBrokerClientService.Connect();
        await _siteBrokerControllerService.Connect();
        await _siteBrokerDataConsumer.Connect();
    }
}