using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wup.Works.SiteBroker.Client.Interfaces;
using Wup.Works.SiteBroker.Client.Models;
using Wup.Works.SiteBroker.Client.Models.Enums;

namespace Wup.Works.SiteBroker.Client.Demo.Mes;

/// <summary>
/// Interactive console demo that simulates an MES / upstream orchestrator. It uses the public
/// <see cref="ISiteBrokerControllerService"/> to push jobs (load order, prepare batch, run batch
/// variant) to a machine and to observe the responses the machine sends back.
///
/// The whole point of this demo is that <b>you</b> decide via the console when and which request is
/// sent — including a number of deliberate error scenarios.
/// </summary>
internal sealed class MesSimulatorService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMilliseconds(750);
    private static readonly TimeSpan TimeoutPollInterval = TimeSpan.FromSeconds(2);

    private readonly ISiteBrokerControllerService _controller;
    private readonly ITelemetryConsumerService _telemetryConsumer;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<MesSimulatorService> _logger;

    private readonly string _targetMachine;
    private readonly TimeSpan _responseTimeout;

    private readonly ConcurrentDictionary<Guid, TrackedRequest> _requests = new();
    private TrackedRequest? _lastRequest;

    public MesSimulatorService(
        ISiteBrokerControllerService controller,
        ITelemetryConsumerService telemetryConsumer,
        IConfiguration configuration,
        IHostApplicationLifetime lifetime,
        ILogger<MesSimulatorService> logger)
    {
        _controller = controller;
        _telemetryConsumer = telemetryConsumer;
        _lifetime = lifetime;
        _logger = logger;

        _targetMachine = configuration["SiteBrokerOptions:MachineNumber"] ?? "DEMO-01";
        var timeoutSeconds = configuration.GetValue("Demo:ResponseTimeoutSeconds", 15);
        _responseTimeout = TimeSpan.FromSeconds(timeoutSeconds);

        _controller.OrderLoadedResponse += OnOrderLoaded;
        _controller.BatchPreparedResponse += OnBatchPrepared;
        _controller.BatchVariantExecutedResponse += OnBatchVariantExecuted;
        _controller.OnlineModeChangedResponse += OnOnlineModeChanged;
        _telemetryConsumer.TelemetryReceived += OnTelemetryReceived;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run the blocking console loop on a background thread so host shutdown is never blocked
        // waiting for the user to press a key.
        _ = Task.Run(() => RunMenuLoop(stoppingToken), CancellationToken.None);
        _ = Task.Run(() => MonitorTimeouts(stoppingToken), CancellationToken.None);

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _controller.OrderLoadedResponse -= OnOrderLoaded;
        _controller.BatchPreparedResponse -= OnBatchPrepared;
        _controller.BatchVariantExecutedResponse -= OnBatchVariantExecuted;
        _controller.OnlineModeChangedResponse -= OnOnlineModeChanged;
        _telemetryConsumer.TelemetryReceived -= OnTelemetryReceived;

        return base.StopAsync(cancellationToken);
    }

    private async Task RunMenuLoop(CancellationToken token)
    {
        // Give the hosted SiteBrokerWorker a moment to connect/subscribe before the first prompt.
        await DelaySafeAsync(StartupDelay, token);

        PrintHeader();
        PrintMenu();

        while (!token.IsCancellationRequested)
        {
            var choice = ConsoleUi.Prompt("\nMES> ");
            if (choice is null)
            {
                // Input stream closed (e.g. piped/non-interactive). Keep the process alive.
                await DelaySafeAsync(Timeout.InfiniteTimeSpan, token);
                return;
            }

            try
            {
                switch (choice.Trim().ToLowerInvariant())
                {
                    case "1":
                        await SendLoadOrder(_targetMachine, askFilename: true);
                        break;
                    case "2":
                        await SendPrepareBatch(_targetMachine, askVariant: true);
                        break;
                    case "3":
                        await SendRunBatchVariant(_targetMachine, askOrderId: true);
                        break;
                    case "4":
                        ListRequests();
                        break;
                    case "5":
                        await ClearRequest();
                        break;
                    case "6":
                        await ErrorScenarioMenu();
                        break;
                    case "q":
                    case "quit":
                    case "exit":
                        ConsoleUi.Info("Stopping MES demo ...");
                        _lifetime.StopApplication();
                        return;
                    case "":
                        break;
                    default:
                        ConsoleUi.Warn("Unknown input.");
                        break;
                }
            }
            catch (Exception ex)
            {
                ConsoleUi.Error($"Execution error: {ex.Message}");
                _logger.LogDebug(ex, "Unhandled error in MES menu loop");
            }
        }
    }

    #region Sending requests

    private async Task SendLoadOrder(string machine, bool askFilename)
    {
        var orderId = ConsoleUi.PromptGuid("OrderId");

        string? filename = null;
        if (askFilename)
        {
            var raw = ConsoleUi.Prompt("Filename (optional, legacy) [Enter = none]: ");
            filename = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
        }

        Track(DemoRequestKind.Order, orderId, machine);
        await _controller.SendLoadOrderRequest(machine, orderId, filename);
        ConsoleUi.Success($"-> LoadOrder sent to {machine}: OrderId={orderId}, Filename={filename ?? "-"}");
    }

    private async Task SendPrepareBatch(string machine, bool askVariant)
    {
        var batchId = ConsoleUi.PromptGuid("BatchId");

        var orderId = PromptOptionalGuid("OrderId (optional, legacy) [Enter = none]: ");

        string? variant = null;
        if (askVariant)
        {
            var raw = ConsoleUi.Prompt("Variant/PSLV (e.g. External-1) [Enter = none -> interactive]: ");
            variant = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
        }

        Track(DemoRequestKind.Batch, batchId, machine);
        await _controller.SendPrepareBatchRequest(machine, batchId, orderId, variant);
        ConsoleUi.Success($"-> PrepareBatch sent to {machine}: BatchId={batchId}, OrderId={orderId?.ToString() ?? "-"}, Variant={variant ?? "-"}");
    }

    private async Task SendRunBatchVariant(string machine, bool askOrderId)
    {
        var batchVariantId = ConsoleUi.PromptGuid("BatchVariantId");

        var batchId = PromptOptionalGuid("BatchId (optional, legacy) [Enter = none]: ");

        Guid? orderId = null;
        if (askOrderId)
        {
            // Manual workstations look the document up in their own DB, so the orchestrator must
            // include the OrderId in the produce payload (see integration manual section 8.3).
            orderId = PromptOptionalGuid("OrderId (for manual workstations) [Enter = none]: ");
        }

        Track(DemoRequestKind.BatchVariant, batchVariantId, machine);
        await _controller.SendExecuteBatchVariantRequest(machine, batchVariantId, batchId, orderId);
        ConsoleUi.Success($"-> RunBatchVariant sent to {machine}: BatchVariantId={batchVariantId}, BatchId={batchId?.ToString() ?? "-"}, OrderId={orderId?.ToString() ?? "-"}");
    }

    #endregion

    #region Error scenarios

    private async Task ErrorScenarioMenu()
    {
        ConsoleUi.Write(
            "\n--- Error scenarios ---\n" +
            "  [1] LoadOrder to an UNKNOWN machine number (nobody answers -> timeout)\n" +
            "  [2] PrepareBatch WITHOUT variant (non-interactive mode -> machine may report an abort)\n" +
            "  [3] RunBatchVariant WITHOUT BatchId/OrderId (manual workstation cannot find the document)\n" +
            "  [4] Resend the last request (duplicate / replay)\n" +
            "  [0] back",
            ConsoleColor.Yellow);

        var choice = ConsoleUi.Prompt("Error scenario> ");
        switch (choice?.Trim())
        {
            case "1":
                var unknownMachine = ConsoleUi.Prompt("Unknown machine number [Enter = UNKNOWN-99]: ");
                unknownMachine = string.IsNullOrWhiteSpace(unknownMachine) ? "UNKNOWN-99" : unknownMachine.Trim();
                await SendLoadOrder(unknownMachine, askFilename: false);
                ConsoleUi.Warn($"No client is registered for {unknownMachine} -> a timeout is expected.");
                break;
            case "2":
                var batchId = ConsoleUi.PromptGuid("BatchId");
                Track(DemoRequestKind.Batch, batchId, _targetMachine);
                await _controller.SendPrepareBatchRequest(_targetMachine, batchId, null, variant: null);
                ConsoleUi.Warn($"-> PrepareBatch sent WITHOUT variant: BatchId={batchId}. The machine may reject this case.");
                break;
            case "3":
                var bvId = ConsoleUi.PromptGuid("BatchVariantId");
                Track(DemoRequestKind.BatchVariant, bvId, _targetMachine);
                await _controller.SendExecuteBatchVariantRequest(_targetMachine, bvId, batchId: null, orderId: null);
                ConsoleUi.Warn($"-> RunBatchVariant sent WITHOUT BatchId/OrderId: BatchVariantId={bvId}.");
                break;
            case "4":
                await ResendLast();
                break;
            case "0":
            case null:
            case "":
                break;
            default:
                ConsoleUi.Warn("Unknown selection.");
                break;
        }
    }

    private async Task ResendLast()
    {
        var last = _lastRequest;
        if (last is null)
        {
            ConsoleUi.Warn("No request has been sent yet.");
            return;
        }

        switch (last.Kind)
        {
            case DemoRequestKind.Order:
                await _controller.SendLoadOrderRequest(last.MachineNumber, last.Id, null);
                break;
            case DemoRequestKind.Batch:
                await _controller.SendPrepareBatchRequest(last.MachineNumber, last.Id, null, null);
                break;
            case DemoRequestKind.BatchVariant:
                await _controller.SendExecuteBatchVariantRequest(last.MachineNumber, last.Id, null, null);
                break;
        }

        ConsoleUi.Warn($"-> Duplicate sent: {last.KindLabel} Id={last.Id}. Receivers must react idempotently.");
    }

    #endregion

    private async Task ClearRequest()
    {
        var id = ConsoleUi.PromptGuid("Which request id to withdraw (clear retained command)");
        if (!_requests.TryGetValue(id, out var request))
        {
            ConsoleUi.Warn("No tracked request with this id - clearing will be attempted anyway.");
        }

        var kind = request?.Kind ?? AskKind();
        var machine = request?.MachineNumber ?? _targetMachine;

        switch (kind)
        {
            case DemoRequestKind.Order:
                await _controller.ClearOrderLoadedResponse(machine, id);
                break;
            case DemoRequestKind.Batch:
                await _controller.ClearBatchPreparedResponse(machine, id);
                break;
            case DemoRequestKind.BatchVariant:
                await _controller.ClearBatchVariantExecutedResponse(machine, id);
                break;
        }

        ConsoleUi.Info($"Retained command for {kind} {id} on {machine} cleared.");
    }

    private static DemoRequestKind AskKind()
    {
        var raw = ConsoleUi.Prompt("Kind? [o]rder / [b]atch / [v]ariant: ")?.Trim().ToLowerInvariant();
        return raw switch
        {
            "b" => DemoRequestKind.Batch,
            "v" => DemoRequestKind.BatchVariant,
            _ => DemoRequestKind.Order
        };
    }

    #region Response handlers

    private void OnOrderLoaded(object? sender, OrderStatusEventArgs e)
        => HandleResponse(e.OrderId, e.Status.ToString(),
            isTerminal: e.Status is OrderStatus.Imported or OrderStatus.Aborted,
            $"OrderLoaded: OrderId={e.OrderId}, Status={e.Status}");

    private void OnBatchPrepared(object? sender, BatchStatusEventArgs e)
        => HandleResponse(e.BatchId, e.Status.ToString(),
            isTerminal: e.Status is BatchStatus.Ready or BatchStatus.Aborted,
            $"BatchPrepared: BatchId={e.BatchId}, Status={e.Status}");

    private void OnBatchVariantExecuted(object? sender, RunStatusEventArgs e)
        => HandleResponse(e.BatchVariantId, e.Status.ToString(),
            isTerminal: e.Status is RunStatus.Done or RunStatus.Aborted,
            $"BatchVariantExecuted: BatchVariantId={e.BatchVariantId}, Status={e.Status}");

    private void OnOnlineModeChanged(object? sender, OnlineModeStatusEventArgs e)
    {
        if (!string.Equals(e.MachineNumber, _targetMachine, StringComparison.OrdinalIgnoreCase))
        {
            // Controller subscribes with a wildcard machine number, so ignore other machines.
            return;
        }

        ConsoleUi.Incoming($"<- Online mode of machine {e.MachineNumber}: {e.Status}");
    }

    private void OnTelemetryReceived(object? sender, TelemetryReceivedEventArgs e)
    {
        if (e.Removed)
        {
            ConsoleUi.Incoming($"<- Telemetry {e.Topic.Path}: <removed>");
            return;
        }

        ConsoleUi.Incoming($"<- Telemetry {e.Topic.Path} = {e.Topic.Payload.Value} [{e.Topic.Payload.Type.ToString().ToLowerInvariant()}]");
    }

    private void HandleResponse(Guid id, string statusText, bool isTerminal, string message)
    {
        if (!_requests.TryGetValue(id, out var request))
        {
            // Response for something we never requested (other client, stray/late message, ...).
            ConsoleUi.Warn($"<- Unexpected/foreign response (not tracked): {message}");
            return;
        }

        request.LastStatus = statusText;
        request.Completed = isTerminal;

        var late = request.TimeoutWarned ? " [LATE response after timeout]" : string.Empty;
        ConsoleUi.Incoming($"<- {message}{late}");
    }

    #endregion

    private void Track(DemoRequestKind kind, Guid id, string machine)
    {
        var request = new TrackedRequest { Kind = kind, Id = id, MachineNumber = machine };
        _requests[id] = request;
        _lastRequest = request;
    }

    private void ListRequests()
    {
        if (_requests.IsEmpty)
        {
            ConsoleUi.Info("No requests sent yet.");
            return;
        }

        ConsoleUi.Write("\n--- Sent requests ---", ConsoleColor.White);
        foreach (var r in _requests.Values.OrderBy(r => r.SentAt))
        {
            var age = (int)(DateTime.Now - r.SentAt).TotalSeconds;
            var state = r.Completed ? "completed" : $"open ({age}s)";
            ConsoleUi.Write($"  {r.KindLabel,-16} {r.Id}  ->  {r.LastStatus}  [{state}]  @{r.MachineNumber}");
        }
    }

    private async Task MonitorTimeouts(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await DelaySafeAsync(TimeoutPollInterval, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            foreach (var r in _requests.Values)
            {
                if (r.Completed || r.TimeoutWarned)
                {
                    continue;
                }

                if (DateTime.Now - r.SentAt > _responseTimeout)
                {
                    r.TimeoutWarned = true;
                    ConsoleUi.Error($"! TIMEOUT: No (final) response for {r.KindLabel} {r.Id} after {_responseTimeout.TotalSeconds:0}s.");
                }
            }
        }
    }

    /// <summary>
    /// Prompts for an optional GUID. Returns <c>null</c> for blank or unparseable input (these IDs
    /// are legacy/optional, so an invalid entry is silently treated as "not provided").
    /// </summary>
    private static Guid? PromptOptionalGuid(string label)
    {
        var raw = ConsoleUi.Prompt(label);
        return !string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw.Trim(), out var parsed)
            ? parsed
            : null;
    }

    /// <summary>
    /// Awaits a delay but swallows the cancellation that is raised during host shutdown, so callers
    /// can continue to their own graceful-exit logic.
    /// </summary>
    private static async Task DelaySafeAsync(TimeSpan delay, CancellationToken token)
    {
        try
        {
            await Task.Delay(delay, token);
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested - swallow.
        }
    }

    private void PrintHeader()
    {
        ConsoleUi.Write(
            "\n==================================================================\n" +
            "  SiteBroker DEMO  —  MES / Orchestrator (job sender)\n" +
            $"  Target machine: {_targetMachine}\n" +
            "==================================================================",
            ConsoleColor.Magenta);
        ConsoleUi.Info("This demo sends jobs to a machine. Start the machine demo in parallel.");
    }

    private static void PrintMenu()
    {
        ConsoleUi.Write(
            "\nMenu:\n" +
            "  [1] Load order          (LoadOrder)\n" +
            "  [2] Prepare batch       (PrepareBatch)\n" +
            "  [3] Run batch variant   (RunBatchVariant)\n" +
            "  [4] Show sent requests & status\n" +
            "  [5] Withdraw request    (clear retained command)\n" +
            "  [6] Error scenarios\n" +
            "  [q] Quit",
            ConsoleColor.White);
    }
}
