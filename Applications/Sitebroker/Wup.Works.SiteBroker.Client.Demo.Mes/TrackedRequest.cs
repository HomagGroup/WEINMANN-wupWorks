namespace Wup.Works.SiteBroker.Client.Demo.Mes;

/// <summary>
/// Bookkeeping for a request that the simulated MES/orchestrator has sent to a machine.
/// Used to correlate asynchronously arriving responses and to detect missing responses (timeouts).
/// </summary>
internal sealed class TrackedRequest
{
    public required DemoRequestKind Kind { get; init; }

    public required Guid Id { get; init; }

    public required string MachineNumber { get; init; }

    public DateTime SentAt { get; } = DateTime.Now;

    public string LastStatus { get; set; } = "(waiting for response)";

    public bool Completed { get; set; }

    public bool TimeoutWarned { get; set; }

    public string KindLabel => Kind switch
    {
        DemoRequestKind.Order => "LoadOrder",
        DemoRequestKind.Batch => "PrepareBatch",
        DemoRequestKind.BatchVariant => "RunBatchVariant",
        _ => Kind.ToString()
    };
}
