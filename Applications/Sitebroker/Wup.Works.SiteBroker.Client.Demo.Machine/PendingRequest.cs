namespace Wup.Works.SiteBroker.Client.Demo.Machine;

/// <summary>
/// A command received from the orchestrator that is waiting for the user to decide which response
/// to send (and when).
/// </summary>
internal sealed class PendingRequest
{
    public required int Index { get; init; }

    public required DemoRequestKind Kind { get; init; }

    public required Guid Id { get; init; }

    public Guid? RelatedOrderId { get; init; }

    public Guid? RelatedBatchId { get; init; }

    public string? Variant { get; init; }

    public string? Filename { get; init; }

    public DateTime ReceivedAt { get; } = DateTime.Now;

    /// <summary>
    /// Current lifecycle phase the operator has driven this request to. Starts at "Requested"
    /// (the orchestrator asked, nothing has been answered yet) and advances as responses are sent.
    /// </summary>
    public string Phase { get; set; } = "Requested";

    /// <summary>
    /// Position within the guided "happy path" (0 = first recommended step). Used to suggest the
    /// next sensible response when the operator just presses [Enter].
    /// </summary>
    public int StepIndex { get; set; }

    public string KindLabel => Kind switch
    {
        DemoRequestKind.Order => "LoadOrder",
        DemoRequestKind.Batch => "PrepareBatch",
        DemoRequestKind.BatchVariant => "RunBatchVariant",
        _ => Kind.ToString()
    };

    /// <summary>
    /// Short, operator-facing description of what is expected next for this kind of request.
    /// </summary>
    public string RecommendedAction => Kind switch
    {
        DemoRequestKind.Order => "Import document  ->  recommended: Preparing, then Imported",
        DemoRequestKind.Batch => "Prepare batch    ->  recommended: Preparing, then Ready",
        DemoRequestKind.BatchVariant => "Produce variant  ->  recommended: Active, then Done",
        _ => string.Empty
    };

    public string Describe()
    {
        var details = Kind switch
        {
            DemoRequestKind.Order => $"Filename={Filename ?? "-"}",
            DemoRequestKind.Batch => $"OrderId={RelatedOrderId?.ToString() ?? "-"}, Variant={Variant ?? "-"}",
            DemoRequestKind.BatchVariant => $"BatchId={RelatedBatchId?.ToString() ?? "-"}",
            _ => string.Empty
        };

        return $"[{Index}] {KindLabel,-16} {Id}  ({details})  - state: {Phase}";
    }
}
