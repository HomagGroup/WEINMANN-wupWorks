using System.Text.Json.Serialization;
using Wup.Works.SiteBroker.Client.Models.Enums;

namespace Wup.Works.SiteBroker.Client.Models.Payload;

/// <summary>
/// The minimal single-value envelope used by every telemetry topic.
/// </summary>
public class TelemetryPayloadDto
{
    /// <summary>The value type of the signal (<c>number</c> or <c>string</c>).</summary>
    [JsonPropertyName("type")]
    public TelemetryValueType Type { get; set; }

    /// <summary>The current value of the signal addressed by the topic.</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>When the value was captured on the machine (ISO 8601, UTC).</summary>
    [JsonPropertyName("timestampUtc")]
    public DateTime TimestampUtc { get; set; }
}
