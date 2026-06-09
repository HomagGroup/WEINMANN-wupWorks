using Microsoft.Extensions.Hosting;
using Wup.Works.SiteBroker.Client.Interfaces;

namespace Wup.Works.SiteBroker.Client.Services.BackgroundWorkers;

public class SiteBrokerWorker : BackgroundService
{
    private readonly ISiteBrokerClientService _siteBrokerClientService;
    private readonly ISiteBrokerControllerService _siteBrokerControllerService;

    public SiteBrokerWorker(ISiteBrokerClientService siteBrokerClientService, ISiteBrokerControllerService siteBrokerControllerService)
    {
        _siteBrokerClientService = siteBrokerClientService;
        _siteBrokerControllerService = siteBrokerControllerService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _siteBrokerClientService.Connect();
        await _siteBrokerControllerService.Connect();
    }
}