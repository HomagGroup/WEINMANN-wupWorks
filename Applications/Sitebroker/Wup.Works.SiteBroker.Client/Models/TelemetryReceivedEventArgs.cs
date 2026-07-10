namespace Wup.Works.SiteBroker.Client.Models;

/// <summary>
/// Carries a received telemetry message. An empty retained payload arrives with
/// <see cref="Removed"/> == <c>true</c>.
/// </summary>
public class TelemetryReceivedEventArgs : EventArgs
{
    /// <summary>The telemetry value (path + value + timestamp).</summary>
    public required TelemetryTopic Topic { get; init; }

    /// <summary>
    /// <c>true</c> when an empty retained payload was received, meaning the signal was removed.
    /// </summary>
    public bool Removed { get; init; }
}
