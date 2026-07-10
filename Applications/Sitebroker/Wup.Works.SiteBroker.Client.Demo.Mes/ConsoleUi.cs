namespace Wup.Works.SiteBroker.Client.Demo.Mes;

/// <summary>
/// Minimal thread-safe console helper so that asynchronously arriving MQTT responses and the
/// interactive menu do not garble each other's output.
/// </summary>
internal static class ConsoleUi
{
    private static readonly object Gate = new();

    public static void Write(string message, ConsoleColor? color = null)
    {
        lock (Gate)
        {
            if (color.HasValue)
            {
                Console.ForegroundColor = color.Value;
            }

            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    public static void Info(string message) => Write(message, ConsoleColor.Gray);

    public static void Success(string message) => Write(message, ConsoleColor.Green);

    public static void Warn(string message) => Write(message, ConsoleColor.Yellow);

    public static void Error(string message) => Write(message, ConsoleColor.Red);

    public static void Incoming(string message) => Write(message, ConsoleColor.Cyan);

    /// <summary>
    /// Reads a line, blocking. Returns <c>null</c> when the input stream was closed.
    /// </summary>
    public static string? Prompt(string label)
    {
        lock (Gate)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(label);
            Console.ResetColor();
        }

        return Console.ReadLine();
    }

    /// <summary>
    /// Reads a GUID. An empty input returns a freshly generated GUID (convenience for the demo).
    /// </summary>
    public static Guid PromptGuid(string label)
    {
        while (true)
        {
            var raw = Prompt($"{label} [Enter = new GUID]: ");
            if (string.IsNullOrWhiteSpace(raw))
            {
                var generated = Guid.NewGuid();
                Info($"   -> generated GUID {generated}");
                return generated;
            }

            if (Guid.TryParse(raw.Trim(), out var parsed))
            {
                return parsed;
            }

            Warn("   Invalid GUID, please try again.");
        }
    }
}
