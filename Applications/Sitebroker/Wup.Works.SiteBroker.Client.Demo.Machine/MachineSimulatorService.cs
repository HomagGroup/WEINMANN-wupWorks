using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wup.Works.SiteBroker.Client.Interfaces;
using Wup.Works.SiteBroker.Client.Models;
using Wup.Works.SiteBroker.Client.Models.Enums;
using static Wup.Works.SiteBroker.Client.Demo.Machine.StatusOptionConsole;

namespace Wup.Works.SiteBroker.Client.Demo.Machine;

/// <summary>
/// Interactive console demo that simulates a machine / client. It uses the public
/// <see cref="ISiteBrokerClientService"/> to receive commands (load order, prepare batch, run batch
/// variant) from an orchestrator and lets <b>you</b> decide via the console when and which response
/// to send back — including a number of deliberate error scenarios.
/// </summary>
internal sealed class MachineSimulatorService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMilliseconds(750);

    private readonly ISiteBrokerClientService _client;
    private readonly ISiteBrokerDataPublisher _data;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<MachineSimulatorService> _logger;

    private readonly string _machineNumber;

    private readonly object _gate = new();
    private readonly List<PendingRequest> _pending = new();
    private int _indexCounter;

    private bool _online = true;

    public MachineSimulatorService(
        ISiteBrokerClientService client,
        ISiteBrokerDataPublisher data,
        IConfiguration configuration,
        IHostApplicationLifetime lifetime,
        ILogger<MachineSimulatorService> logger)
    {
        _client = client;
        _data = data;
        _lifetime = lifetime;
        _logger = logger;

        _machineNumber = configuration["SiteBrokerOptions:MachineNumber"] ?? "DEMO-01";

        _client.LoadOrderRequested += OnLoadOrderRequested;
        _client.PrepareBatchRequested += OnPrepareBatchRequested;
        _client.RunBatchVariantRequested += OnRunBatchVariantRequested;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run the blocking console loop on a background thread so host shutdown is never blocked
        // waiting for the user to press a key.
        _ = Task.Run(() => RunMenuLoop(stoppingToken), CancellationToken.None);

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _client.LoadOrderRequested -= OnLoadOrderRequested;
        _client.PrepareBatchRequested -= OnPrepareBatchRequested;
        _client.RunBatchVariantRequested -= OnRunBatchVariantRequested;

        return base.StopAsync(cancellationToken);
    }

    private async Task RunMenuLoop(CancellationToken token)
    {
        await DelaySafeAsync(StartupDelay, token);

        // Announce online mode on startup (online mode is a prerequisite for the orchestrator to
        // send PrepareBatch/RunBatchVariant commands).
        try
        {
            await _client.SendOnlineModeResponse(_online);
        }
        catch (Exception ex)
        {
            ConsoleUi.Error($"Could not send online mode: {ex.Message}");
        }

        PrintHeader();
        PrintMenu();

        while (!token.IsCancellationRequested)
        {
            PrintStatusLine();

            var choice = ConsoleUi.Prompt("\nMACHINE> ");
            if (choice is null)
            {
                await DelaySafeAsync(Timeout.InfiniteTimeSpan, token);
                return;
            }

            try
            {
                switch (choice.Trim().ToLowerInvariant())
                {
                    case "":
                        // The linear default action: work the next open command through its flow.
                        await ProcessPending();
                        break;
                    case "o":
                        await ToggleOnlineMode();
                        break;
                    case "u":
                        await SendUnsolicitedResponse();
                        break;
                    case "d":
                        await PublishDemoData();
                        break;
                    case "l":
                        ListPending();
                        break;
                    case "q":
                    case "quit":
                    case "exit":
                        ConsoleUi.Info("Stopping machine demo ...");
                        _lifetime.StopApplication();
                        return;
                    default:
                        ConsoleUi.Warn("Unknown input.");
                        break;
                }
            }
            catch (Exception ex)
            {
                ConsoleUi.Error($"Execution error: {ex.Message}");
                _logger.LogDebug(ex, "Unhandled error in machine menu loop");
            }
        }
    }

    private void PrintStatusLine()
    {
        int count;
        PendingRequest? next;
        lock (_gate)
        {
            count = _pending.Count;
            next = _pending.OrderBy(r => r.Index).FirstOrDefault();
        }

        if (count == 0)
        {
            ConsoleUi.Info("Waiting for jobs from the MES ... (once one arrives: [Enter] to process it)");
        }
        else
        {
            ConsoleUi.Write(
                $"{count} open job(s) in the queue. Next: {next!.KindLabel} #{next.Index}  ->  [Enter] to process.",
                ConsoleColor.Cyan);
        }
    }

    #region Incoming command handlers

    private async void OnLoadOrderRequested(object? sender, OrderStatusEventArgs e)
    {
        try
        {
            var request = Add(new PendingRequest
            {
                Index = NextIndex(),
                Kind = DemoRequestKind.Order,
                Id = e.OrderId,
                Filename = e.Filename
            });

            ConsoleUi.Incoming($"<- LoadOrder received (retained): OrderId={e.OrderId}, Filename={e.Filename ?? "-"}");
            await ClearReceivedCommand(request);
            Hint(request);
        }
        catch (Exception ex)
        {
            LogHandlerError(ex, "LoadOrder");
        }
    }

    private async void OnPrepareBatchRequested(object? sender, BatchStatusEventArgs e)
    {
        try
        {
            var request = Add(new PendingRequest
            {
                Index = NextIndex(),
                Kind = DemoRequestKind.Batch,
                Id = e.BatchId,
                RelatedOrderId = e.OrderId,
                Variant = e.Variant
            });

            ConsoleUi.Incoming($"<- PrepareBatch received (retained): BatchId={e.BatchId}, OrderId={e.OrderId?.ToString() ?? "-"}, Variant={e.Variant ?? "-"}");
            if (e.Variant is null)
            {
                ConsoleUi.Warn("   No variant/PSLV set -> in non-interactive mode this would be an error case (abort possible).");
            }

            await ClearReceivedCommand(request);
            Hint(request);
        }
        catch (Exception ex)
        {
            LogHandlerError(ex, "PrepareBatch");
        }
    }

    private async void OnRunBatchVariantRequested(object? sender, RunStatusEventArgs e)
    {
        try
        {
            var request = Add(new PendingRequest
            {
                Index = NextIndex(),
                Kind = DemoRequestKind.BatchVariant,
                Id = e.BatchVariantId,
                RelatedBatchId = e.BatchId,
                RelatedOrderId = e.OrderId
            });

            ConsoleUi.Incoming($"<- RunBatchVariant received (retained): BatchVariantId={e.BatchVariantId}, BatchId={e.BatchId?.ToString() ?? "-"}, OrderId={e.OrderId?.ToString() ?? "-"}");
            if (e.OrderId is { } documentId)
            {
                ConsoleUi.Info($"   [Workstation] Loading document/order {documentId} from the local database and showing it at the workstation.");
            }

            if (!_online)
            {
                ConsoleUi.Warn("   Machine is OFFLINE - per the specification the orchestrator should not send jobs here.");
            }

            await ClearReceivedCommand(request);
            Hint(request);
        }
        catch (Exception ex)
        {
            LogHandlerError(ex, "RunBatchVariant");
        }
    }

    private void LogHandlerError(Exception ex, string command)
    {
        ConsoleUi.Error($"Error while handling incoming {command}: {ex.Message}");
        _logger.LogDebug(ex, "Unhandled error while handling incoming {Command}", command);
    }

    private static void Hint(PendingRequest request)
    {
        ConsoleUi.Info($"   Next step: {request.RecommendedAction}");
        ConsoleUi.Info("   -> Press [Enter] to process this job in guided mode.");
    }

    #endregion

    #region Responding

    private async Task ProcessPending()
    {
        // Linear / FIFO: always work the oldest open command first and keep going until the queue
        // is empty or the operator defers a request with [x].
        while (true)
        {
            PendingRequest? request;
            lock (_gate)
            {
                request = _pending.OrderBy(r => r.Index).FirstOrDefault();
            }

            if (request is null)
            {
                ConsoleUi.Info("No open jobs - waiting for the next command from the MES.");
                return;
            }

            var resolved = await GuidedRespond(request);
            if (!resolved)
            {
                // Operator left the dialog with [x]; stop draining so they can do something else.
                return;
            }
        }
    }

    private async Task<bool> GuidedRespond(PendingRequest request)
    {
        switch (request.Kind)
        {
            case DemoRequestKind.Order:
                return await Guided(
                    request,
                    "Load order (LoadOrder)",
                    new (int key, OrderStatus value, string label)[]
                    {
                        (1, OrderStatus.Preparing, "Preparing"),
                        (2, OrderStatus.Imported, "Imported"),
                        (3, OrderStatus.Aborted, "Aborted")
                    },
                    happyPath: new[] { OrderStatus.Preparing, OrderStatus.Imported },
                    isTerminal: s => s is OrderStatus.Imported or OrderStatus.Aborted,
                    send: s => _client.SendOrderLoadedResponse(request.Id, s));

            case DemoRequestKind.Batch:
            {
                var orderId = RequireGuid("OrderId for the response", request.RelatedOrderId);
                return await Guided(
                    request,
                    "Prepare batch (PrepareBatch)",
                    new (int key, BatchStatus value, string label)[]
                    {
                        (1, BatchStatus.Incomplete, "Incomplete"),
                        (2, BatchStatus.Preparing, "Preparing"),
                        (3, BatchStatus.Ready, "Ready"),
                        (4, BatchStatus.Aborted, "Aborted")
                    },
                    happyPath: new[] { BatchStatus.Preparing, BatchStatus.Ready },
                    isTerminal: s => s is BatchStatus.Ready or BatchStatus.Aborted,
                    send: s => _client.SendBatchPreparedResponse(request.Id, orderId, s));
            }

            case DemoRequestKind.BatchVariant:
            {
                var batchId = RequireGuid("BatchId for the response", request.RelatedBatchId);
                // The OrderId now travels with the produce command (RunStatusEventArgs.OrderId). If the
                // orchestrator supplied it, reuse it; otherwise ask explicitly.
                var orderId = RequireGuid("OrderId for the response", request.RelatedOrderId);
                return await Guided(
                    request,
                    "Run batch variant (RunBatchVariant)",
                    new (int key, RunStatus value, string label)[]
                    {
                        (1, RunStatus.Inactive, "Inactive"),
                        (2, RunStatus.Active, "Active"),
                        (3, RunStatus.Done, "Done"),
                        (4, RunStatus.Aborted, "Aborted")
                    },
                    happyPath: new[] { RunStatus.Active, RunStatus.Done },
                    isTerminal: s => s is RunStatus.Done or RunStatus.Aborted,
                    send: s => _client.SendBatchVariantExecutedResponse(request.Id, batchId, orderId, s));
            }

            default:
                return true;
        }
    }

    /// <summary>
    /// Drives the operator through the lifecycle of a single request. Shows a lifecycle bar with the
    /// current phase, recommends the next sensible status ([Enter] picks it), accepts an explicit
    /// status number, or [i]gnore / [x] back. Loops for intermediate (non-terminal) steps.
    /// Returns <c>true</c> if the request was resolved (terminal or ignored), <c>false</c> if the
    /// operator deferred it with [x].
    /// </summary>
    private async Task<bool> Guided<TEnum>(
        PendingRequest request,
        string title,
        IReadOnlyList<(int key, TEnum value, string label)> options,
        IReadOnlyList<TEnum> happyPath,
        Func<TEnum, bool> isTerminal,
        Func<TEnum, Task> send) where TEnum : struct, Enum
    {
        while (true)
        {
            TEnum? suggested = request.StepIndex < happyPath.Count ? happyPath[request.StepIndex] : null;

            PrintLifecycle(title, request, options, happyPath);
            ConsoleUi.Write(BuildOptionLine(options, suggested.HasValue ? LabelOf(options, suggested.Value) : null), ConsoleColor.White);

            var input = ConsoleUi.Prompt("Step> ")?.Trim().ToLowerInvariant();

            TEnum chosen;
            if (string.IsNullOrEmpty(input))
            {
                if (suggested is null)
                {
                    ConsoleUi.Warn("No recommended step left - please choose a status number, [i] or [x].");
                    continue;
                }

                chosen = suggested.Value;
            }
            else if (input == "x")
            {
                ConsoleUi.Info("Job deferred - it stays open and can be processed later with [Enter].");
                return false;
            }
            else if (input == "i")
            {
                Ignore(request);
                return true;
            }
            else if (int.TryParse(input, out var num) && TryGetOption(options, num, out chosen))
            {
                // chosen assigned by TryGetOption
            }
            else
            {
                ConsoleUi.Warn("Invalid input ([Enter]=recommended, number=status, [i]=ignore, [x]=defer).");
                continue;
            }

            await send(chosen);

            var label = LabelOf(options, chosen);
            request.Phase = label;
            var pathIndex = IndexOf(happyPath, chosen);
            if (pathIndex >= 0)
            {
                request.StepIndex = pathIndex + 1;
            }

            ConsoleUi.Success($"-> Response sent: {title} = {label} (Id={request.Id})");

            if (isTerminal(chosen))
            {
                Remove(request);
                ConsoleUi.Success($"   Process completed: {label}. Job [{request.Index}] closed.");
                return true;
            }

            ConsoleUi.Info("   Intermediate step sent - the job stays open. [Enter] sends the next recommended step, [x] defers it.");
        }
    }

    private Guid RequireGuid(string label, Guid? known)
    {
        if (known.HasValue && known.Value != Guid.Empty)
        {
            return known.Value;
        }

        ConsoleUi.Warn($"   {label} is not known.");
        var value = ConsoleUi.PromptGuid(label, Guid.Empty);
        if (value == Guid.Empty)
        {
            ConsoleUi.Warn("   Guid.Empty will be sent (intentional error case).");
        }

        return value;
    }

    private void Ignore(PendingRequest request)
    {
        Remove(request);
        ConsoleUi.Warn($"Request {request.KindLabel} {request.Id} ignored - NO response will be sent (simulates a timeout at the MES).");
    }

    #endregion

    #region Error / edge-case actions

    private async Task ToggleOnlineMode()
    {
        _online = !_online;
        await _client.SendOnlineModeResponse(_online);
        ConsoleUi.Success($"Online mode now: {(_online ? "Online" : "Offline")}");
        if (!_online)
        {
            ConsoleUi.Warn("Offline: responding to jobs would be an error case per the specification.");
        }
    }

    private async Task SendUnsolicitedResponse()
    {
        ConsoleUi.Write(
            "Send an unsolicited/foreign response (e.g. for an id that was never requested):\n" +
            "  [o] OrderLoaded  [b] BatchPrepared  [v] BatchVariantExecuted",
            ConsoleColor.Yellow);
        var kindRaw = ConsoleUi.Prompt("Kind> ")?.Trim().ToLowerInvariant();
        var id = ConsoleUi.PromptGuid("Id", Guid.NewGuid());

        switch (kindRaw)
        {
            case "o":
                await _client.SendOrderLoadedResponse(id, OrderStatus.Imported);
                break;
            case "b":
                await _client.SendBatchPreparedResponse(id, Guid.Empty, BatchStatus.Ready);
                break;
            case "v":
                await _client.SendBatchVariantExecutedResponse(id, Guid.Empty, Guid.Empty, RunStatus.Done);
                break;
            default:
                ConsoleUi.Warn("Cancelled.");
                return;
        }

        ConsoleUi.Warn($"-> Unsolicited response sent (Id={id}). It appears at the MES as a 'foreign/unexpected' response.");
    }

    #endregion

    #region Helpers

    private async Task ClearReceivedCommand(PendingRequest request)
    {
        // Taking ownership of a retained command means clearing it on the broker, otherwise it would
        // be re-delivered on the next (re)connect. This is a fixed part of the machine flow.
        switch (request.Kind)
        {
            case DemoRequestKind.Order:
                await _client.ClearLoadOrderRequest(Constants.Orchestrator, request.Id);
                break;
            case DemoRequestKind.Batch:
                await _client.ClearPrepareBatchRequest(Constants.Orchestrator, request.Id);
                break;
            case DemoRequestKind.BatchVariant:
                await _client.ClearExecuteBatchVariantRequest(Constants.Orchestrator, request.Id);
                break;
        }

        ConsoleUi.Info($"   Retained command for {request.KindLabel} {request.Id} taken over and cleared.");
    }

    private PendingRequest Add(PendingRequest request)
    {
        lock (_gate)
        {
            _pending.Add(request);
        }

        return request;
    }

    private void Remove(PendingRequest request)
    {
        lock (_gate)
        {
            _pending.Remove(request);
        }
    }

    private int NextIndex() => Interlocked.Increment(ref _indexCounter);

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

    private void ListPending()
    {
        lock (_gate)
        {
            if (_pending.Count == 0)
            {
                ConsoleUi.Info("No open requests.");
                return;
            }

            ConsoleUi.Write("\n--- Open jobs (queue) ---", ConsoleColor.White);
            foreach (var r in _pending.OrderBy(r => r.Index))
            {
                ConsoleUi.Write("  " + r.Describe());
            }
        }
    }

    private async Task PublishDemoData()
    {
        ConsoleUi.Write(
            "\nPublish machine data:\n" +
            "  [1] State = Working      [2] State = Idle        [3] State = Error\n" +
            "  [4] Parts +1             [5] Cycles +1           [6] Meter +12.5\n" +
            "  [7] Program = 'P-4711'   [8] Set error entry     [9] Clear error entry\n" +
            "  [s] Storage fill level   [Enter] Cancel",
            ConsoleColor.White);

        var choice = ConsoleUi.Prompt("DATA> ");
        switch (choice?.Trim().ToLowerInvariant())
        {
            case "1":
                await _data.PublishMachineState(MachineState.Working);
                ConsoleUi.Success("-> state = Working (3)");
                break;
            case "2":
                await _data.PublishMachineState(MachineState.Idle);
                ConsoleUi.Success("-> state = Idle (2)");
                break;
            case "3":
                await _data.PublishMachineState(MachineState.Error);
                ConsoleUi.Success("-> state = Error (4)");
                break;
            case "4":
                await _data.PublishParts(++_parts);
                ConsoleUi.Success($"-> parts = {_parts}");
                break;
            case "5":
                await _data.PublishCycles(++_cycles);
                ConsoleUi.Success($"-> cycles = {_cycles}");
                break;
            case "6":
                _meter += 12.5;
                await _data.PublishMeter(_meter);
                ConsoleUi.Success($"-> meter = {_meter}");
                break;
            case "7":
                await _data.PublishProgram("P-4711");
                ConsoleUi.Success("-> program = P-4711");
                break;
            case "8":
                await _data.PublishError("E-1042", "Vacuum too low");
                ConsoleUi.Success("-> error[E-1042] = 'Vacuum too low'");
                break;
            case "9":
                await _data.RemoveIndexed(DataCategory.Machine, DataGroup.Error, "E-1042");
                ConsoleUi.Success("-> error[E-1042] cleared (empty retained payload)");
                break;
            case "s":
                await _data.PublishStorage("S1", currentAmount: 42, maxAmount: 100, currentMaterialType: "PUR-Glue");
                ConsoleUi.Success("-> storage[S1] = 42/100, material PUR-Glue");
                break;
            default:
                ConsoleUi.Info("Cancelled.");
                break;
        }
    }

    private long _parts;
    private long _cycles;
    private double _meter;

    private void PrintHeader()
    {
        ConsoleUi.Write(
            "\n==================================================================\n" +
            "  SiteBroker DEMO  —  Machine / Client\n" +
            $"  Machine number: {_machineNumber}   Online: {_online}\n" +
            "==================================================================",
            ConsoleColor.Magenta);
        ConsoleUi.Info("This demo receives jobs. Start the MES demo in parallel and send jobs from there.");

        ConsoleUi.Write(
            "\nMachine workflow (linear - how a job moves through the system):\n" +
            "  1) Go online ................... [o]  (prerequisite for the orchestrator to send jobs)\n" +
            "  2) LoadOrder ................... import document   : Preparing -> Imported\n" +
            "  3) PrepareBatch ................ prepare batch     : Preparing -> Ready\n" +
            "  4) RunBatchVariant ............. produce variant   : Active    -> Done\n" +
            "\nIncoming commands are taken over automatically (retained command cleared) and put into a\n" +
            "queue. With [Enter] you process the next job in order, guided step by step: the demo shows\n" +
            "the lifecycle, the current state and the recommended next step. [Enter] sends the\n" +
            "recommended step.",
            ConsoleColor.Gray);
    }

    private void PrintMenu()
    {
        ConsoleUi.Write(
            "\nControls:\n" +
            "  [Enter] Process the next queued job in guided mode\n" +
            $"  [o]     Toggle online mode       (currently: {(_online ? "Online" : "Offline")})\n" +
            "  [l]     Show open jobs\n" +
            "  [d]     Publish machine data (state / counters / error / storage)\n" +
            "  [u]     Error case: send an unsolicited/foreign response\n" +
            "  [q]     Quit",
            ConsoleColor.White);
    }

    #endregion
}
