using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wup.Works.SiteBroker.Client;
using Wup.Works.SiteBroker.Client.Demo.Mes;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

// Registers the public site-broker API. For the MES/orchestrator role we only use the
// ISiteBrokerControllerService that this call provides.
builder.Services.AddSiteBroker(builder.Configuration);
builder.Services.AddHostedService<MesSimulatorService>();

var host = builder.Build();

var hostname = builder.Configuration["Mqtt:Hostname"] ?? "localhost";
var port = builder.Configuration["Mqtt:Port"] ?? "1883";

// AddSiteBroker connects the MQTT client synchronously while the host is built/started and blocks
// up to "ConnectTimeoutInSeconds" (>= 30s) if no broker is reachable. Tell the user up front so a
// missing broker does not look like a freeze.
ConsoleUi.Info($"Connecting to MQTT broker {hostname}:{port} ... (may take up to 30s if no broker is running)");

await host.RunAsync();
