namespace Wup.Works.SiteBroker.Client.Demo.Machine;

/// <summary>
/// Stateless, presentation-only helpers for the guided console dialog: rendering the lifecycle bar
/// and the option line, plus small generic lookups over the available status options. Kept separate
/// from <see cref="MachineSimulatorService"/> so the service only owns the interaction flow.
/// </summary>
internal static class StatusOptionConsole
{
    public static void PrintLifecycle<TEnum>(
        string title,
        PendingRequest request,
        IReadOnlyList<(int key, TEnum value, string label)> options,
        IReadOnlyList<TEnum> happyPath) where TEnum : struct, Enum
    {
        var phases = new List<string> { "Requested" };
        phases.AddRange(happyPath.Select(v => LabelOf(options, v)));

        var bar = string.Join("  ->  ", phases.Select(p =>
            string.Equals(p, request.Phase, StringComparison.OrdinalIgnoreCase) ? $">[{p}]<" : p));

        var abortLabel = options.Select(o => o.label).FirstOrDefault(l => string.Equals(l, "Aborted", StringComparison.OrdinalIgnoreCase));

        ConsoleUi.Write($"\n=== Guided response [{request.Index}] {title} ===", ConsoleColor.Magenta);
        ConsoleUi.Write($"   Id:              {request.Id}", ConsoleColor.Gray);
        if (request.RelatedOrderId is { } oid)
        {
            ConsoleUi.Write($"   Document/Order:  {oid}", ConsoleColor.Gray);
        }

        ConsoleUi.Write($"   Flow:            {bar}" + (abortLabel is not null ? $"     (abort anytime: {abortLabel})" : ""), ConsoleColor.White);
        ConsoleUi.Write($"   Current state:   {request.Phase}", ConsoleColor.Cyan);
    }

    public static string BuildOptionLine<TEnum>(
        IReadOnlyList<(int key, TEnum value, string label)> options,
        string? suggestedLabel) where TEnum : struct, Enum
    {
        var opts = string.Join("  ", options.Select(o => $"[{o.key}] {o.label}"));
        var enterHint = suggestedLabel is not null ? $"[Enter] recommended: {suggestedLabel}   " : string.Empty;
        return $"   {enterHint}{opts}   [i] Ignore   [x] back";
    }

    public static string LabelOf<TEnum>(IReadOnlyList<(int key, TEnum value, string label)> options, TEnum value)
        where TEnum : struct, Enum
        => options.First(o => EqualityComparer<TEnum>.Default.Equals(o.value, value)).label;

    public static bool TryGetOption<TEnum>(IReadOnlyList<(int key, TEnum value, string label)> options, int key, out TEnum value)
        where TEnum : struct, Enum
    {
        foreach (var o in options)
        {
            if (o.key == key)
            {
                value = o.value;
                return true;
            }
        }

        value = default;
        return false;
    }

    public static int IndexOf<TEnum>(IReadOnlyList<TEnum> sequence, TEnum value) where TEnum : struct, Enum
    {
        for (var i = 0; i < sequence.Count; i++)
        {
            if (EqualityComparer<TEnum>.Default.Equals(sequence[i], value))
            {
                return i;
            }
        }

        return -1;
    }
}
