using Wup.Works.SiteBroker.Client.Models;

namespace Wup.Works.SiteBroker.Client.Interfaces;

/// <summary>
/// Consumes machine telemetry published to the telemetry channel.
/// </summary>
public interface ITelemetryConsumerService
{
    /// <summary>
    /// Fires for any telemetry value. An empty retained payload arrives with
    /// <see cref="TelemetryReceivedEventArgs.Removed"/> == <c>true</c>.
    /// </summary>
    event EventHandler<TelemetryReceivedEventArgs> TelemetryReceived;

    /// <summary>Connect to the site broker and subscribe to the telemetry channel.</summary>
    /// <returns>An asynchronous task.</returns>
    Task Connect();

    /// <summary>Unsubscribe from the telemetry channel and disconnect.</summary>
    /// <returns>An asynchronous task.</returns>
    Task Disconnect();
}
