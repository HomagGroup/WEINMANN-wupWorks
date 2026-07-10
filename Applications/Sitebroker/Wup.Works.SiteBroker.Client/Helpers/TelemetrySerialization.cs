using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wup.Works.SiteBroker.Client.Helpers;

/// <summary>
/// Shared JSON serializer options for telemetry payloads, used by both the producer and the
/// consumer so the wire format (lower-case <see cref="Models.Enums.TelemetryValueType"/>) stays in sync.
/// </summary>
internal static class TelemetrySerialization
{
    public static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
