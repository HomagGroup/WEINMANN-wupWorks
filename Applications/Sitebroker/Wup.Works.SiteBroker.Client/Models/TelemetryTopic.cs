using System.Globalization;
using Wup.Works.SiteBroker.Client.Models.Payload;

namespace Wup.Works.SiteBroker.Client.Models;

/// <summary>
/// A single telemetry value addressed by its full topic path. Used both when producing
/// (build the path, set the payload, publish) and when consuming (the parsed incoming message).
/// </summary>
public class TelemetryTopic
{
    /// <summary>The full MQTT topic path, e.g. <c>M01/telemetry/machine/state</c>.</summary>
    public required string Path { get; init; }

    /// <summary>The payload envelope carried on the topic (type, value, timestamp).</summary>
    public TelemetryPayloadDto Payload { get; set; } = new();

    /// <summary>
    /// Convert <see cref="TelemetryPayloadDto.Value"/> to the requested primitive type using
    /// invariant culture (e.g. <c>GetValueAs&lt;int&gt;()</c>, <c>GetValueAs&lt;DateTime&gt;()</c>).
    /// </summary>
    public T GetValueAs<T>()
    {
        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)Convert.ChangeType(Payload.Value, target, CultureInfo.InvariantCulture);
    }
}
